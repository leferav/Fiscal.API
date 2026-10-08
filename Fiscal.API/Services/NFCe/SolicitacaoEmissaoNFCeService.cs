
using System.Text.Json;
using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.NFe;
using Microsoft.EntityFrameworkCore;

namespace Fiscal.API.Services.NFCe;

public class SolicitacaoEmissaoNFCeService
{
    private readonly FiscalDbContext _context;
    private readonly NumeracaoNFCeService _numeracaoService;
    private readonly NFCeBuilder _nfceBuilder;
    private readonly NFCeXmlPreparacaoService _xmlService;

    public SolicitacaoEmissaoNFCeService(
        FiscalDbContext context,
        NumeracaoNFCeService numeracaoService,
        NFCeBuilder nfceBuilder,
        NFCeXmlPreparacaoService xmlService)
    {
        _context = context;
        _numeracaoService = numeracaoService;
        _nfceBuilder = nfceBuilder;
        _xmlService = xmlService;
    }

    public async Task<SolicitacaoEmissao> CriarAsync(
        Guid empresaId,
        List<ItemFiscal> itensFiscais,
        DestinatarioRequest? destinatario = null,
        CancellationToken cancellationToken = default)
    {
        if (empresaId == Guid.Empty)
            throw new ArgumentException("Empresa inválida.");

        if (itensFiscais == null || itensFiscais.Count == 0)
            throw new InvalidOperationException(
                "Informe pelo menos um item fiscal.");

        // O chamador deve fornecer itens já validados.
        await using var transacao =
            await _context.Database.BeginTransactionAsync(
                cancellationToken);

        var configuracao = await _numeracaoService
            .ObterConfiguracaoComBloqueioAsync(
                empresaId, cancellationToken);

        var numero = checked(
            (int)configuracao.ProximoNumeroNFCe);

        var empresa = await _context.Empresas
            .Include(x => x.ConfiguracaoFiscal)
            .SingleOrDefaultAsync(
                x => x.Id == empresaId,
                cancellationToken);

        if (empresa == null || !empresa.Ativo)
            throw new InvalidOperationException(
                "Empresa não encontrada ou inativa.");

        if (configuracao.Ambiente != 2)
            throw new InvalidOperationException(
                "Nesta fase, somente homologação é permitida.");

        var nfce = _nfceBuilder.Criar(
            itensFiscais,
            empresa,
            configuracao,
            destinatario,
            numeroReservado: numero);

        var payload = _xmlService.Preparar(
            nfce, empresaId);

        if (payload.Numero != numero ||
            payload.Serie != configuracao.SerieNFCe ||
            payload.Ambiente != configuracao.Ambiente)
        {
            throw new InvalidOperationException(
                "Dados fiscais divergentes na preparação.");
        }

        var solicitacao = new SolicitacaoEmissao
        {
            Id = Guid.NewGuid(),
            EmpresaId = empresaId,
            Modelo = 65,
            Ambiente = configuracao.Ambiente,
            Serie = configuracao.SerieNFCe,
            Numero = numero,
            Status = "PENDENTE",
            PayloadJson = JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web)),
            CriadoEm = DateTime.UtcNow
        };

        _context.SolicitacoesEmissao.Add(solicitacao);

        configuracao.ProximoNumeroNFCe =
            checked((long)numero + 1);

        await _context.SaveChangesAsync(
            cancellationToken);

        await transacao.CommitAsync(
            cancellationToken);

        return solicitacao;
    }
}
