using DFe.Classes.Flags;
using Fiscal.Agent.Services.NFe;
using NFe.Classes.Servicos.Tipos;
using NFe.Servicos;
using NFe.Utils;
using NFe.Utils.InformacoesSuplementares;
using NFe.Utils.NFe;

namespace Fiscal.Agent.Services.NFCe;

public class NFCeEmissaoService
{
    private readonly ZeusConfigurationFactory _configFactory;

    public NFCeEmissaoService(ZeusConfigurationFactory configFactory)
    {
        _configFactory = configFactory;
    }

    // Mantido para compatibilidade com o codigo existente.
    public string AssinarXml(string xml, string caminhoPfx, string senha)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML da NFC-e nao informado.");

        var configuracao = _configFactory.Criar(caminhoPfx, senha);
        configuracao.ModeloDocumento = ModeloDocumento.NFCe;

        var nfce = new global::NFe.Classes.NFe().CarregarDeXmlString(xml);
        nfce.Assina(configuracao);
        return nfce.ObterXmlString();
    }

    // Este metodo deve ser chamado somente com CSC e ID CSC da mesma empresa/ambiente.
    public ResultadoEmissaoNFCe Emitir(
        string xml,
        string caminhoPfx,
        string senha,
        string cscId,
        string csc)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentException("XML da NFC-e nao informado.");
        if (string.IsNullOrWhiteSpace(cscId) || string.IsNullOrWhiteSpace(csc))
            throw new ArgumentException("CSC e ID CSC sao obrigatorios para gerar o QR Code.");

        var configuracao = _configFactory.Criar(caminhoPfx, senha);
        configuracao.ModeloDocumento = ModeloDocumento.NFCe;

        var nfce = new global::NFe.Classes.NFe().CarregarDeXmlString(xml);

        // Mesmo fluxo de assinatura e QR Code ja usado na Fiscal.API.
        nfce.Assina(configuracao);
        nfce.infNFeSupl = new global::NFe.Classes.infNFeSupl();
        nfce.infNFeSupl.urlChave = nfce.infNFeSupl.ObterUrlConsulta(
            nfce, VersaoQrCode.QrCodeVersao3);
        nfce.infNFeSupl.qrCode = nfce.infNFeSupl.ObterUrlQrCode(
            nfce, VersaoQrCode.QrCodeVersao3, cscId, csc, configuracao.Certificado);
        nfce.Valida(configuracao);

        var xmlEnvio = nfce.ObterXmlString();

        using var servicoNFe = new ServicosNFe(configuracao);
        var retorno = servicoNFe.NFeAutorizacao(
            1,
            IndicadorSincronizacao.Sincrono,
            new List<global::NFe.Classes.NFe> { nfce },
            false);

        var resposta = retorno.Retorno;
        var protocolo = resposta.protNFe;
        var cStat = protocolo?.infProt?.cStat ?? resposta.cStat;
        var autorizada = protocolo?.infProt?.cStat == 100;

        string? xmlAutorizado = null;
        if (autorizada && protocolo != null)
        {
            var proc = new global::NFe.Classes.nfeProc
            {
                versao = "4.00",
                NFe = nfce,
                protNFe = protocolo
            };
            xmlAutorizado = proc.ObterXmlString();
        }

        return new ResultadoEmissaoNFCe
        {
            Sucesso = autorizada,
            CStat = cStat,
            Motivo = protocolo?.infProt?.xMotivo ?? resposta.xMotivo,
            ChaveAcesso = protocolo?.infProt?.chNFe ?? nfce.infNFe.Id?.Replace("NFe", ""),
            Protocolo = protocolo?.infProt?.nProt,
            XmlEnvio = xmlEnvio,
            XmlRetorno = retorno.RetornoStr,
            XmlAutorizado = xmlAutorizado
        };
    }
}

public class ResultadoEmissaoNFCe
{
    public bool Sucesso { get; set; }
    public int? CStat { get; set; }
    public string? Motivo { get; set; }
    public string? ChaveAcesso { get; set; }
    public string? Protocolo { get; set; }
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public string? XmlAutorizado { get; set; }
}
