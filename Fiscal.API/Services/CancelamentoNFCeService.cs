using DFe.Classes.Flags;
using Fiscal.API.Data;
using Fiscal.API.Services.CertificadoDigital;
using Fiscal.API.Services.NFe;
using Microsoft.EntityFrameworkCore;
using NFe.Servicos;
using System.Security.Cryptography;

namespace Fiscal.API.Services;

public sealed class CancelamentoNFCeService
{
    private readonly FiscalDbContext _db;
    private readonly ZeusConfigurationFactory _factory;
    private readonly CertificadoDigitalService _certificados;
    private readonly ILogger<CancelamentoNFCeService> _logger;

    public CancelamentoNFCeService(FiscalDbContext db,
        ZeusConfigurationFactory factory,
        CertificadoDigitalService certificados,
        ILogger<CancelamentoNFCeService> logger)
    {
        _db = db;
        _factory = factory;
        _certificados = certificados;
        _logger = logger;
    }

    public async Task<object> CancelarAsync(Guid empresaId, Guid notaId, string? justificativa)
    {
        justificativa = justificativa?.Trim();
        if (string.IsNullOrWhiteSpace(justificativa) || justificativa.Length < 15 || justificativa.Length > 255)
            throw new ArgumentException("A justificativa deve conter de 15 a 255 caracteres.");

        var nota = await _db.NotasFiscais.AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notaId && n.EmpresaId == empresaId && n.Modelo == 65);
        if (nota == null) throw new KeyNotFoundException("NFC-e não encontrada.");
        if (nota.Status != "AUTORIZADA")
            throw new InvalidOperationException("Somente NFC-e AUTORIZADA pode solicitar cancelamento.");
        if (nota.ChaveAcesso?.Length != 44 || string.IsNullOrWhiteSpace(nota.Protocolo))
            throw new InvalidOperationException("NFC-e sem chave ou protocolo de autorização válido.");

        var empresa = await _db.Empresas.AsNoTracking().Include(e => e.ConfiguracaoFiscal)
            .FirstOrDefaultAsync(e => e.Id == empresaId && e.Ativo);
        if (empresa?.ConfiguracaoFiscal == null)
            throw new InvalidOperationException("Empresa ou configuração fiscal inválida.");
        if (empresa.ConfiguracaoFiscal.Ambiente != nota.Ambiente)
            throw new InvalidOperationException("Ambiente da nota diferente da configuração fiscal atual.");

        var agora = DateTime.UtcNow;
        var reservou = await _db.NotasFiscais
            .Where(n => n.Id == notaId && n.EmpresaId == empresaId && n.Modelo == 65 &&
                        n.Status == "AUTORIZADA" && n.CancelamentoSolicitadoEm == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.Status, "CANCELAMENTO_EM_ANDAMENTO")
                .SetProperty(n => n.JustificativaCancelamento, justificativa)
                .SetProperty(n => n.CancelamentoSolicitadoEm, agora)
                .SetProperty(n => n.AtualizadoEm, agora));
        if (reservou != 1)
            throw new InvalidOperationException("Cancelamento já solicitado ou nota indisponível.");

        await RegistrarLogSeguroAsync(empresaId, notaId, "NFCe_CANCELAMENTO_SOLICITADO", "Information",
            "Cancelamento reservado para envio à SEFAZ.", nota.ChaveAcesso, nota.Numero, nota.Serie);

        // A partir daqui, falhas sem resposta conclusiva exigem verificação antes de reenviar.
        try
        {
            var certificado = await _certificados.ObterCertificadoAsync(empresaId);
            try
            {
                var cfg = _factory.CriarComCertificadoEmMemoria(certificado.Pfx, certificado.Senha);
                cfg.ModeloDocumento = ModeloDocumento.NFCe;
                using var servico = new ServicosNFe(cfg);
                var resposta = servico.RecepcaoEventoCancelamento(
                    1, 1, nota.Protocolo!, nota.ChaveAcesso!, justificativa,
                    nota.ChaveAcesso!.Substring(6, 14));

                var evento = resposta.Retorno.retEvento?
                    .Select(e => e.infEvento)
                    .FirstOrDefault(e => e != null && e.chNFe == nota.ChaveAcesso);
                var codigo = evento?.cStat;
                var protocolo = evento?.nProt;
                var confirmado = (codigo == 135 || codigo == 155) &&
                                 !string.IsNullOrWhiteSpace(protocolo);

                // Um evento retornado com rejeição explícita é conclusivo. 573 indica
                // duplicidade de evento e exige consulta antes de assumir o resultado.
                // 135/155 sem protocolo também não comprovam cancelamento.
                var rejeitado = evento != null && codigo != 135 && codigo != 155 && codigo != 573;
                var status = confirmado ? "CANCELADA" :
                             rejeitado ? "AUTORIZADA" : "CANCELAMENTO_INDETERMINADO";
                var motivo = evento?.xMotivo ?? resposta.Retorno.xMotivo;
                var instante = DateTime.UtcNow;

                await using var tx = await _db.Database.BeginTransactionAsync();
                var alteradas = await _db.NotasFiscais
                    .Where(n => n.Id == notaId && n.EmpresaId == empresaId &&
                                n.Status == "CANCELAMENTO_EM_ANDAMENTO")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.Status, status)
                        .SetProperty(n => n.CStatCancelamento, codigo)
                        .SetProperty(n => n.MotivoCancelamento, motivo)
                        .SetProperty(n => n.ProtocoloCancelamento, confirmado ? protocolo : null)
                        .SetProperty(n => n.XmlCancelamentoEnvio, resposta.EnvioStr)
                        .SetProperty(n => n.XmlCancelamentoRetorno, resposta.RetornoStr)
                        .SetProperty(n => n.CanceladoEm, confirmado ? instante : (DateTime?)null)
                        // Rejeição explícita libera nova solicitação manual; resultado
                        // indeterminado permanece bloqueado contra retransmissão.
                        .SetProperty(n => n.CancelamentoSolicitadoEm, rejeitado ? (DateTime?)null : agora)
                        .SetProperty(n => n.AtualizadoEm, instante));
                if (alteradas != 1)
                    throw new InvalidOperationException("Estado da NFC-e mudou durante o cancelamento; verifique antes de reenviar.");

                if (confirmado)
                    await _db.Vendas.Where(v => v.EmpresaId == empresaId && v.NotaFiscalId == notaId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(v => v.Status, "CANCELADA")
                            .SetProperty(v => v.AtualizadoEm, instante));
                await tx.CommitAsync();

                await RegistrarLogSeguroAsync(empresaId, notaId,
                    confirmado ? "NFCe_CANCELAMENTO_AUTORIZADO" :
                    rejeitado ? "NFCe_CANCELAMENTO_REJEITADO" : "NFCe_CANCELAMENTO_INDETERMINADO",
                    confirmado ? "Information" : "Warning",
                    motivo, nota.ChaveAcesso, nota.Numero, nota.Serie,
                    codigo ?? resposta.Retorno.cStat, confirmado ? protocolo : null);

                return new
                {
                    confirmado,
                    status,
                    cStatLote = resposta.Retorno.cStat,
                    cStatEvento = codigo,
                    motivo,
                    protocoloCancelamento = confirmado ? protocolo : null
                };
            }
            finally { CryptographicOperations.ZeroMemory(certificado.Pfx); }
        }
        catch (Exception ex)
        {
            await RegistrarLogSeguroAsync(empresaId, notaId, "NFCe_CANCELAMENTO_ERRO", "Error",
                "Falha no processamento do cancelamento; verificar situação na SEFAZ antes de reenviar.",
                nota.ChaveAcesso, nota.Numero, nota.Serie, detalhes: ex.ToString());
            try
            {
                await _db.NotasFiscais
                    .Where(n => n.Id == notaId && n.EmpresaId == empresaId &&
                                n.Status == "CANCELAMENTO_EM_ANDAMENTO")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.Status, "CANCELAMENTO_INDETERMINADO")
                        .SetProperty(n => n.AtualizadoEm, DateTime.UtcNow));
            }
            catch (Exception atualizacaoEx)
            {
                _logger.LogError(atualizacaoEx,
                    "Falha ao marcar cancelamento indeterminado para NotaFiscalId {NotaFiscalId}", notaId);
            }
            throw;
        }
    }

    // Registro isolado: falha de auditoria não muda o resultado fiscal.
    private async Task RegistrarLogSeguroAsync(Guid empresaId, Guid notaId, string evento,
        string nivel, string? mensagem, string? chave, long numero, int serie,
        int? cStat = null, string? protocolo = null, string? detalhes = null)
    {
        try
        {
            var id = Guid.NewGuid();
            var criadoEm = DateTime.UtcNow;
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
                                                            INSERT INTO logs_fiscais
                                                            (""Id"", ""EmpresaId"", ""NotaFiscalId"", ""Evento"", ""Nivel"",
                                                             ""Mensagem"", ""DetalhesErro"", ""ChaveAcesso"", ""Numero"", ""Serie"",
                                                             ""CStat"", ""Protocolo"", ""CriadoEm"")
                                                            VALUES
                                                            ({id}, {empresaId}, {notaId}, {evento}, {nivel},
                                                             {mensagem}, {detalhes}, {chave}, {numero}, {serie},
                                                             {cStat}, {protocolo}, {criadoEm})");
        }
        catch (Exception logEx)
        {
            _logger.LogWarning(logEx,
                "Falha ao persistir log fiscal {Evento} para EmpresaId {EmpresaId}, NotaFiscalId {NotaFiscalId}",
                evento, empresaId, notaId);
        }
    }

}
