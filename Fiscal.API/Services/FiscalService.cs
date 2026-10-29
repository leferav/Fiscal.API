using DFe.Classes.Flags;
using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Models.NFe;
using Fiscal.API.Services.CertificadoDigital;
using Fiscal.API.Services.NFCe;
using Fiscal.API.Services.NFe;
using Microsoft.EntityFrameworkCore;
using NFe.Classes;
using NFe.Classes.Servicos.Tipos;
using NFe.Servicos;
using NFe.Servicos.Retorno;
using NFe.Utils;
using NFe.Utils.Enderecos;
using NFe.Utils.InformacoesSuplementares;
using NFe.Utils.NFe;

namespace Fiscal.API.Services
{
    public class FiscalService
    {
        private readonly ILogger<FiscalService> _logger;
        private readonly ZeusConfigurationFactory _configFactory;
        private readonly NFeBuilder _nfeBuilder;
        private readonly NFCeBuilder _nfceBuilder;
        private readonly NFeXmlService _nfeXmlService;
        private readonly FiscalDbContext _context;
        private readonly CertificadoDigitalService _certificadoDigitalService;
        private readonly NumeracaoFiscalService _numeracaoFiscalService;                    

        public FiscalService(
            ZeusConfigurationFactory configFactory,
            NFeBuilder nfeBuilder,
            NFeXmlService nfeXmlService,
            NFCeBuilder nfceBuilder,
            FiscalDbContext context,
            CertificadoDigitalService certificadoDigitalService,
            NumeracaoFiscalService numeracaoFiscalService,
            ILogger<FiscalService> logger)
        {
            _logger = logger;
            _configFactory = configFactory;
            _nfeBuilder = nfeBuilder;
            _nfeXmlService = nfeXmlService;
            _nfceBuilder = nfceBuilder;
            _context = context;
            _certificadoDigitalService = certificadoDigitalService;
            _numeracaoFiscalService = numeracaoFiscalService;
        }

        #region NF-e
        public object ConsultarStatusNFe()
        {
            var configuracao = _configFactory.Criar();

            using var servicoNFe = new ServicosNFe(configuracao);

            var retorno = servicoNFe.NfeStatusServico();

            return new
            {
                XmlEnvio = retorno.EnvioStr,
                XmlRetorno = retorno.RetornoStr
            };
        }

        public object GerarNFe(EmitirNFeRequest request)
        {
            var nfe = _nfeBuilder.Criar(request);

            var xml = _nfeXmlService.AssinarXml(nfe);

            return new
            {
                mensagem = "XML da NF-e gerado e assinado com sucesso.",
                versao = nfe.infNFe.versao,
                numero = nfe.infNFe.ide.nNF,
                serie = nfe.infNFe.ide.serie,
                id = nfe.infNFe.Id,
                xml
            };
        }

        public object AutorizarNFe(EmitirNFeRequest request)
        {
            // Monta a NF-e
            var nfe = _nfeBuilder.Criar(request);

            // Obtém a mesma configuração usada no restante do projeto
            var configuracao = _configFactory.Criar();

            // Assina com o certificado A1
            nfe.Assina(configuracao);

            // Valida contra os schemas
            nfe.Valida(configuracao);

            // Cria o serviço do Zeus
            using var servicoNFe = new ServicosNFe(configuracao);

            // Lote de teste
            var idLote = 1;

            RetornoNFeAutorizacao retorno;

            try
            {
                retorno = servicoNFe.NFeAutorizacao(
                    idLote,
                    IndicadorSincronizacao.Sincrono,
                    new List<global::NFe.Classes.NFe> { nfe },
                    false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "ERRO COMPLETO SEFAZ: " + ex.ToString());

                System.Diagnostics.Debug.WriteLine(
                    "CAUSA PRINCIPAL: " + ex.GetBaseException().Message);

                throw;
            }

            var retEnviNFe = retorno.Retorno;

            var protocolo = retEnviNFe.protNFe;

            return new
            {
                sucesso = protocolo?.infProt?.cStat == 100,

                ambiente = configuracao.tpAmb.ToString(),

                chave = protocolo?.infProt?.chNFe
                        ?? nfe.infNFe.Id?.Replace("NFe", ""),

                cStatLote = retEnviNFe.cStat,
                motivoLote = retEnviNFe.xMotivo,

                cStat = protocolo?.infProt?.cStat,
                motivo = protocolo?.infProt?.xMotivo,

                protocolo = protocolo?.infProt?.nProt
            };
        }

        public object ConsultarNFe(string chave)
        {
            if (string.IsNullOrWhiteSpace(chave))
                throw new Exception("Chave da NF-e não informada.");

            chave = new string(chave.Where(char.IsDigit).ToArray());

            if (chave.Length != 44)
                throw new Exception("A chave da NF-e deve possuir 44 números.");

            var configuracao = _configFactory.Criar();

            using var servicoNFe = new ServicosNFe(configuracao);

            var retorno = servicoNFe.NfeConsultaProtocolo(chave);

            var retConsSitNFe = retorno.Retorno;

            return new
            {
                chave,
                ambiente = configuracao.tpAmb.ToString(),
                cStat = retConsSitNFe.cStat,
                motivo = retConsSitNFe.xMotivo,
                protocolo = retConsSitNFe.protNFe?.infProt?.nProt
            };
        }

        #endregion



        #region NFC-e


        public async Task<object> AutorizarNFCeAsync( EmitirNFeRequest request, List<ItemFiscal>? itensVenda = null, string formaPagamento = "01", Guid? vendaId = null)
        {

            if (vendaId.HasValue && itensVenda == null)
            {
                throw new InvalidOperationException(
                    "A emissão de uma venda pendente exige os itens fiscais da venda.");
            }

            if (vendaId.HasValue && vendaId.Value == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Identificador da venda inválido.");
            }


            Venda? vendaPendente = null;

            if (vendaId.HasValue)
            {
                vendaPendente = await _context.Vendas.AsNoTracking()
                    .FirstOrDefaultAsync(v =>
                        v.Id == vendaId.Value &&
                        v.EmpresaId == request.EmpresaId);

                if (vendaPendente == null)
                {
                    throw new InvalidOperationException(
                        "Venda não encontrada para a empresa informada.");
                }

                if (vendaPendente.Status != "EMITINDO")
                {
                    throw new InvalidOperationException(
                        $"A venda não está reservada para emissão. Status atual: {vendaPendente.Status}");
                }

                if (vendaPendente.NotaFiscalId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Esta venda já possui uma tentativa fiscal vinculada. Consulte a situação da NFC-e antes de tentar novamente.");
                }
            }


            var empresa = await _context.Empresas
                .Include(x => x.ConfiguracaoFiscal)
                .Include(x => x.ConfiguracoesTributarias)
                .FirstOrDefaultAsync(x => x.Id == request.EmpresaId);

            if (empresa is null)
                throw new Exception("Empresa não encontrada.");

            if (!empresa.Ativo)
                throw new Exception("Empresa está inativa.");

            if (empresa.ConfiguracaoFiscal is null)
                throw new Exception(
                    "Empresa não possui configuração fiscal.");

            // Resolve o cliente cadastrado antes de reservar a numeração fiscal.
            // Os dados enviados pelo frontend não substituem os dados oficiais do cadastro.
            DestinatarioRequest? destinatarioEmissao = request.Destinatario;

            if (request.ClienteId.HasValue)
            {
                if (request.ClienteId.Value == Guid.Empty)
                    throw new InvalidOperationException("ClienteId inválido.");

                var cliente = await _context.Clientes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.Id == request.ClienteId.Value &&
                        c.EmpresaId == empresa.Id &&
                        c.Ativo);

                if (cliente == null)
                    throw new InvalidOperationException(
                        "Cliente não encontrado, inativo ou pertencente a outra empresa.");

                destinatarioEmissao = new DestinatarioRequest
                {
                    Nome = cliente.NomeRazaoSocial,
                    CpfCnpj = cliente.CpfCnpj,
                    InscricaoEstadual = cliente.InscricaoEstadual,
                    Logradouro = cliente.Logradouro ?? string.Empty,
                    Numero = cliente.Numero ?? string.Empty,
                    Bairro = cliente.Bairro ?? string.Empty,
                    CodigoMunicipio = cliente.CodigoMunicipioIbge ?? string.Empty,
                    Municipio = cliente.Cidade ?? string.Empty,
                    Uf = cliente.Uf ?? string.Empty,
                    Cep = cliente.Cep ?? string.Empty,
                    Email = cliente.Email
                };
            }

            var configuracaoFiscal = empresa.ConfiguracaoFiscal;

            // A numeração é reservada atomicamente na transação de persistência.
            _context.Entry(configuracaoFiscal).State = EntityState.Detached;

            if (itensVenda == null && (request.Produtos == null || request.Produtos.Count == 0))
            {
                throw new Exception("Informe pelo menos um produto.");
            }

            if (itensVenda != null)
            {
                if (itensVenda.Count == 0)
                    throw new InvalidOperationException(
                        "A venda pendente não possui itens.");

                if (itensVenda.Any(x =>
                    x.ProdutoId == Guid.Empty ||
                    x.Quantidade <= 0 ||
                    x.ValorUnitario <= 0 ||
                    x.ConfiguracaoTributaria == null ||
                    !x.ConfiguracaoTributaria.Ativo))
                {
                    throw new InvalidOperationException(
                        "A venda pendente possui itens ou configurações tributárias inválidas.");
                }
            }

            // IDs dos produtos recebidos pelo PDV
            var itensFiscais = itensVenda ?? new List<ItemFiscal>();

            if (itensVenda == null)
            {
                var produtoIds = request.Produtos
                .Select(x => x.ProdutoId)
                .Distinct()
                .ToList();


                // Carrega produtos + configuração tributária
                var produtosCadastrados = await _context.Produtos
                    .AsNoTracking()
                    .Include(x => x.ConfiguracaoTributaria)
                    .Where(x =>
                        produtoIds.Contains(x.Id) &&
                        x.EmpresaId == empresa.Id &&
                        x.Ativo)
                    .ToListAsync();


                foreach (var itemRequest in request.Produtos)
                {
                    if (itemRequest.ProdutoId == Guid.Empty)
                        throw new Exception("ProdutoId não informado.");

                    if (itemRequest.Quantidade <= 0)
                        throw new Exception(
                            "A quantidade deve ser maior que zero.");

                    var produto = produtosCadastrados
                        .FirstOrDefault(x => x.Id == itemRequest.ProdutoId);

                    if (produto == null)
                    {
                        throw new Exception(
                            $"Produto {itemRequest.ProdutoId} não encontrado, " +
                            "inativo ou pertencente a outra empresa.");
                    }

                    if (produto.ValorVenda <= 0)
                    {
                        throw new Exception(
                            $"O produto {produto.Codigo} não possui valor de venda válido.");
                    }

                    if (produto.ConfiguracaoTributaria == null)
                    {
                        throw new Exception(
                            $"Produto {produto.Codigo} não possui configuração tributária.");
                    }

                    if (!produto.ConfiguracaoTributaria.Ativo)
                    {
                        throw new Exception(
                            $"A configuração tributária do produto {produto.Codigo} está inativa.");
                    }

                    itensFiscais.Add(new ItemFiscal
                    {
                        ProdutoId = produto.Id,
                        Codigo = produto.Codigo,
                        Descricao = produto.Descricao,
                        Ncm = produto.Ncm,
                        Unidade = produto.Unidade,
                        Quantidade = itemRequest.Quantidade,
                        ValorUnitario = produto.ValorVenda,
                        ConfiguracaoTributaria = produto.ConfiguracaoTributaria
                    });
                }
            }

            // Certificado e configuração preparados antes de abrir a transação.
            var dadosCertificado = await _certificadoDigitalService
                .ObterCertificadoAsync(empresa.Id);

            try
            {
                var configuracao = _configFactory.CriarComCertificadoEmMemoria(
                    dadosCertificado.Pfx, dadosCertificado.Senha);
                configuracao.ModeloDocumento = ModeloDocumento.NFCe;

                global::NFe.Classes.NFe? nfce = null;
                NotaFiscal? tentativa = null;

                // A transação protege número + XML + tentativa + vínculo da venda.
                // Nenhuma chamada de rede à SEFAZ ocorre dentro dela.
                await using (var transacao = await _context.Database.BeginTransactionAsync())
                {
                    var numeroReservado = await _numeracaoFiscalService
                        .ReservarProximoNumeroNFCeAsync(empresa.Id);

                    nfce = _nfceBuilder.Criar(
                        itensFiscais, empresa, configuracaoFiscal,
                        destinatarioEmissao, formaPagamento, numeroReservado);

                    var diferenca = DateTimeOffset.UtcNow -
                        nfce.infNFe.ide.dhEmi.ToUniversalTime();
                    if (diferenca > TimeSpan.FromMinutes(4) ||
                        diferenca < TimeSpan.FromMinutes(-1))
                        throw new InvalidOperationException(
                            "Horário de emissão fora da tolerância. Verifique o relógio do servidor.");

                    nfce.Assina(configuracao);
                    nfce.infNFeSupl = new global::NFe.Classes.infNFeSupl();
                    nfce.infNFeSupl.urlChave = nfce.infNFeSupl.ObterUrlConsulta(
                        nfce, VersaoQrCode.QrCodeVersao3);
                    nfce.infNFeSupl.qrCode = nfce.infNFeSupl.ObterUrlQrCode(
                        nfce, VersaoQrCode.QrCodeVersao3,
                        configuracaoFiscal.CscId, configuracaoFiscal.Csc,
                        configuracao.Certificado);
                    nfce.Valida(configuracao);
                    var xmlNfce = nfce.ObterXmlString();

                    tentativa = new NotaFiscal
                    {
                        Id = Guid.NewGuid(),
                        EmpresaId = empresa.Id,
                        Modelo = 65,
                        Serie = nfce.infNFe.ide.serie,
                        Numero = nfce.infNFe.ide.nNF,
                        ChaveAcesso = nfce.infNFe.Id?.Replace("NFe", ""),
                        Ambiente = (short)configuracaoFiscal.Ambiente,
                        Status = "PENDENTE_TRANSMISSAO",
                        ValorProdutos = nfce.infNFe.total.ICMSTot.vProd,
                        ValorTotal = nfce.infNFe.total.ICMSTot.vNF,
                        XmlEnvio = xmlNfce,
                        CriadoEm = DateTime.UtcNow
                    };
                    _context.NotasFiscais.Add(tentativa);

                    if (vendaId.HasValue)
                    {
                        // Revalidação sob transação; a venda já foi reservada pelo endpoint.
                        var venda = await _context.Vendas.FirstOrDefaultAsync(v =>
                            v.Id == vendaId.Value && v.EmpresaId == empresa.Id &&
                            v.Status == "EMITINDO" && v.NotaFiscalId == null);
                        if (venda == null)
                            throw new InvalidOperationException(
                                "Venda não disponível para vinculação fiscal.");
                        venda.NotaFiscalId = tentativa.Id;
                        venda.AtualizadoEm = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    await transacao.CommitAsync();
                }

                // Após o commit, falhas de rede são INDETERMINADAS.
                // A tentativa persiste para consulta pela chave, sem novo número.
                using var servicoNFe = new ServicosNFe(configuracao);

                RetornoNFeAutorizacao retorno;
                try
                {
                    retorno = servicoNFe.NFeAutorizacao(
                        1, IndicadorSincronizacao.Sincrono,
                        new List<global::NFe.Classes.NFe> { nfce! }, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Falha ao transmitir NFC-e. EmpresaId={EmpresaId} NotaFiscalId={NotaFiscalId} VendaId={VendaId} Numero={Numero} Serie={Serie} Chave={Chave}. Resultado SEFAZ indeterminado; consultar antes de reenviar.",
                        empresa.Id, tentativa!.Id, vendaId, tentativa.Numero, tentativa.Serie, tentativa.ChaveAcesso);
                    if (vendaId.HasValue)
                    {
                        await _context.Vendas.Where(v => v.Id == vendaId.Value &&
                            v.EmpresaId == empresa.Id && v.NotaFiscalId == tentativa!.Id)
                            .ExecuteUpdateAsync(s => s
                                .SetProperty(v => v.Status, "VERIFICAR_SEFAZ")
                                .SetProperty(v => v.AtualizadoEm, DateTime.UtcNow));
                    }
                    throw;
                }


                var retEnviNFe = retorno.Retorno;
                var protocolo = retEnviNFe.protNFe;

                var codigoRetorno = protocolo?.infProt?.cStat
                    ?? retEnviNFe.cStat;

                // Autorização exige protocolo válido.
                var autorizada =
                    (codigoRetorno == 100 || codigoRetorno == 150) &&
                    protocolo?.infProt != null &&
                    !string.IsNullOrWhiteSpace(protocolo.infProt.nProt);

                _logger.Log(autorizada ? LogLevel.Information : LogLevel.Warning,
                    "Retorno SEFAZ NFC-e. EmpresaId={EmpresaId} NotaFiscalId={NotaFiscalId} VendaId={VendaId} Numero={Numero} Serie={Serie} Chave={Chave} CStat={CStat} Motivo={Motivo} Protocolo={Protocolo} Autorizada={Autorizada}",
                    empresa.Id, tentativa!.Id, vendaId, tentativa.Numero, tentativa.Serie, tentativa.ChaveAcesso,
                    codigoRetorno, protocolo?.infProt?.xMotivo ?? retEnviNFe.xMotivo, protocolo?.infProt?.nProt, autorizada);

                // Rejeições explícitas.
                // 704: Duplicidade de NFC-e com diferença na chave de acesso.
                var rejeicaoConfirmada =
                    !autorizada &&
                    (
                        codigoRetorno == 704 ||
                        (protocolo?.infProt != null &&
                         codigoRetorno != 100 &&
                         codigoRetorno != 150)
                    );

                // Demais situações permanecem pendentes
                // até análise do retorno ou consulta à SEFAZ.


                string? xmlAutorizado = null;
                if (autorizada && protocolo != null)
                {
                    var proc = new global::NFe.Classes.nfeProc
                    {
                        versao = "4.00", NFe = nfce!, protNFe = protocolo
                    };
                    xmlAutorizado = proc.ObterXmlString();
                }

                tentativa!.Status = autorizada ? "AUTORIZADA" :
                    rejeicaoConfirmada ? "REJEITADA" : "PENDENTE_TRANSMISSAO";
                tentativa.Protocolo = protocolo?.infProt?.nProt;
                tentativa.CStat = codigoRetorno;
                tentativa.XMotivo = protocolo?.infProt?.xMotivo ?? retEnviNFe.xMotivo;
                tentativa.XmlRetorno = retorno.RetornoStr;
                tentativa.XmlAutorizado = xmlAutorizado;
                tentativa.AutorizadoEm = autorizada ? DateTime.UtcNow : null;
                tentativa.AtualizadoEm = DateTime.UtcNow;

                if (vendaId.HasValue)
                {
                    var venda = await _context.Vendas.FirstAsync(v =>
                        v.Id == vendaId.Value && v.EmpresaId == empresa.Id);
                    venda.Status = autorizada ? "EMITIDA" :
                        rejeicaoConfirmada ? "REJEITADA" : "VERIFICAR_SEFAZ";
                    venda.AtualizadoEm = DateTime.UtcNow;
                }
                // Não incrementar ProximoNumeroNFCe aqui: já foi reservado no banco.
                await _context.SaveChangesAsync();

                return new
                {
                    sucesso = autorizada,
                    ambiente = configuracao.tpAmb.ToString(),
                    modelo = "NFC-e 65",
                    chave = protocolo?.infProt?.chNFe ?? nfce!.infNFe.Id?.Replace("NFe", ""),
                    cStatLote = retEnviNFe.cStat,
                    motivoLote = retEnviNFe.xMotivo,
                    cStat = codigoRetorno,
                    motivo = protocolo?.infProt?.xMotivo ?? retEnviNFe.xMotivo,
                    protocolo = protocolo?.infProt?.nProt,
                    numero = nfce.infNFe.ide.nNF,
                    serie = nfce.infNFe.ide.serie
                };
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations
                    .ZeroMemory(dadosCertificado.Pfx);
            }
        }


        public async Task<object> ConsultarNFCeAsync( string chave, Guid empresaId)
        {
            if (string.IsNullOrWhiteSpace(chave))
                throw new ArgumentException("Chave da NFC-e não informada.");

            chave = new string(chave.Where(char.IsDigit).ToArray());

            if (chave.Length != 44)
                throw new ArgumentException("A chave deve possuir 44 números.");

            var empresa = await _context.Empresas
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == empresaId);

            if (empresa == null || !empresa.Ativo)
                throw new InvalidOperationException("Empresa inválida ou inativa.");

            // Recupera o certificado A1 da empresa no Aiven
            var dados = await _certificadoDigitalService
                .ObterCertificadoAsync(empresaId);

            try
            {
                var configuracao = _configFactory
                    .CriarComCertificadoEmMemoria(
                        dados.Pfx,
                        dados.Senha);

                configuracao.ModeloDocumento = ModeloDocumento.NFCe;

                using var servicoNFe = new ServicosNFe(configuracao);

                var retorno = servicoNFe.NfeConsultaProtocolo(chave);

                var resultado = retorno.Retorno;

                return new
                {
                    chave,
                    ambiente = configuracao.tpAmb.ToString(),
                    modelo = "NFC-e 65",
                    cStat = resultado.cStat,
                    motivo = resultado.xMotivo,
                    protocolo = resultado.protNFe?.infProt?.nProt
                };
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations
                    .ZeroMemory(dados.Pfx);
            }
        }


        public async Task<object> ConsultarPendenciasNFCeAsync(Guid empresaId)
        {
            var notas = await _context.NotasFiscais
                .AsNoTracking()
                .Where(n =>
                    n.EmpresaId == empresaId &&
                    n.Modelo == 65 &&
                    n.Status == "PENDENTE_TRANSMISSAO")
                .OrderBy(n => n.CriadoEm)
                .Select(n => new
                {
                    n.Id,
                    n.Numero,
                    n.Serie,
                    n.ChaveAcesso,
                    n.Status,
                    n.CStat,
                    n.XMotivo,
                    n.CriadoEm
                })
                .ToListAsync();

            return new
            {
                quantidade = notas.Count,
                notas
            };
        }

        #endregion

    }
}