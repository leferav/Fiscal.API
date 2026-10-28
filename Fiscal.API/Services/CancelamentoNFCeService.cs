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

    public CancelamentoNFCeService(FiscalDbContext db,
        ZeusConfigurationFactory factory,
        CertificadoDigitalService certificados)
    {
        _db = db;
        _factory = factory;
        _certificados = certificados;
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
        catch
        {
            await _db.NotasFiscais
                .Where(n => n.Id == notaId && n.EmpresaId == empresaId &&
                            n.Status == "CANCELAMENTO_EM_ANDAMENTO")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.Status, "CANCELAMENTO_INDETERMINADO")
                    .SetProperty(n => n.AtualizadoEm, DateTime.UtcNow));
            throw;
        }
    }
}
