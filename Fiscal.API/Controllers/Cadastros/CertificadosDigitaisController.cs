
using DFe.Classes.Entidades;
using DFe.Classes.Flags;
using Fiscal.API.Data;
using Fiscal.API.Models.Database;
using Fiscal.API.Services.CertificadoDigital;
using Fiscal.API.Services.NFe;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NFe.Classes.Informacoes.Identificacao.Tipos;
using NFe.Classes.Servicos.Tipos;
using NFe.Utils;
using NFe.Utils.Enderecos;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Fiscal.API.Controllers.Cadastros;

[ApiController]
[Authorize]
[Route("api/cadastros/certificados-digitais")]
public class CertificadosDigitaisController : ControllerBase
{
    private readonly FiscalDbContext _context;
    private readonly CertificadoCriptografiaService _criptografia;
    private readonly CertificadoDigitalService _certificadoDigitalService;
    private readonly ZeusConfigurationFactory _zeusFactory;
    private readonly ZeusConfigurationFactory _zeusConfigurationFactory;

    public CertificadosDigitaisController(FiscalDbContext context, CertificadoCriptografiaService criptografia, CertificadoDigitalService certificadoDigitalService, ZeusConfigurationFactory zeusFactory, ZeusConfigurationFactory zeusConfigurationFactory)
    {
        _context = context;
        _criptografia = criptografia;
        _certificadoDigitalService = certificadoDigitalService;
        _zeusFactory = zeusFactory;
        _zeusConfigurationFactory = zeusConfigurationFactory;
    }

    [HttpGet("meu")]
    public async Task<IActionResult> ObterMeuCertificado()
    {
        var empresaIdClaim =
            User.FindFirst("empresaId")?.Value;

        if (!Guid.TryParse(empresaIdClaim, out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var certificado = await _context.CertificadosDigitais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .Select(x => new
            {
                x.Id,
                x.Titular,
                x.Cnpj,
                x.Emissor,
                x.Thumbprint,
                x.NumeroSerie,
                x.ValidoDe,
                x.ValidoAte,
                x.Ativo,
                x.NomeArquivo,
                arquivoConfigurado =
                    x.ReferenciaArmazenamento != null &&
                    x.ReferenciaArmazenamento != ""
            })
            .FirstOrDefaultAsync();

        if (certificado == null)
        {
            return Ok(new
            {
                configurado = false,
                mensagem = "Nenhum certificado cadastrado."
            });
        }

        return Ok(new
        {
            configurado = true,
            dados = certificado
        });
    }


    [HttpPost("meu")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> EnviarMeuCertificado( [FromForm] UploadCertificadoDigitalRequest request )
    {
        var empresaIdClaim = User.FindFirst("empresaId")?.Value;

        if (!Guid.TryParse(empresaIdClaim, out var empresaId))
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });

        if (request.Arquivo == null ||
            request.Arquivo.Length == 0 ||
            request.Arquivo.Length > 5 * 1024 * 1024)
            return BadRequest(new
            {
                mensagem = "Informe um certificado de até 5 MB."
            });

        var extensao = Path.GetExtension(request.Arquivo.FileName);

        if (!string.Equals(extensao, ".pfx",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extensao, ".p12",
                StringComparison.OrdinalIgnoreCase))
            return BadRequest(new
            {
                mensagem = "O arquivo deve ser .pfx ou .p12."
            });

        if (string.IsNullOrWhiteSpace(request.Senha))
            return BadRequest(new
            {
                mensagem = "Informe a senha do certificado."
            });

        var empresa = await _context.Empresas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == empresaId);

        if (empresa == null || !empresa.Ativo)
            return BadRequest(new
            {
                mensagem = "Empresa não encontrada ou inativa."
            });

        byte[] bytes;

        using (var memoria = new MemoryStream())
        {
            await request.Arquivo.CopyToAsync(memoria);
            bytes = memoria.ToArray();
        }

        string titular;
        string emissor;
        string thumbprint;
        string numeroSerie;
        DateTime validoDe;
        DateTime validoAte;
        string cnpjEmpresa = new string(
                (empresa.Cnpj ?? "")
                    .Where(char.IsDigit)
                    .ToArray());

        try
        {
            using var certificado = new X509Certificate2(
                bytes,
                request.Senha,
                X509KeyStorageFlags.EphemeralKeySet);

            if (!certificado.HasPrivateKey)
                return BadRequest(new
                {
                    mensagem = "O certificado não possui chave privada."
                });

            var agora = DateTime.UtcNow;

            validoDe = certificado.NotBefore.ToUniversalTime();
            validoAte = certificado.NotAfter.ToUniversalTime();

            if (agora < validoDe || agora > validoAte)
                return BadRequest(new
                {
                    mensagem = "O certificado está fora do prazo de validade."
                });

            titular = certificado.Subject;
            emissor = certificado.Issuer;
            thumbprint = certificado.Thumbprint;
            numeroSerie = certificado.SerialNumber;

            var cnpjCertificado = ExtrairCnpjCertificado(certificado);

            var cnpjConfere =
                !string.IsNullOrWhiteSpace(cnpjCertificado) &&
                cnpjCertificado == cnpjEmpresa;

            if (!cnpjConfere)
            {
                return BadRequest(new
                {
                    mensagem = "O CNPJ do certificado digital não corresponde ao CNPJ da empresa."
                });
            }

            await SalvarCertificadoAsync(
                empresaId,
                request.Arquivo.FileName,
                titular,
                cnpjEmpresa,
                emissor,
                thumbprint,
                numeroSerie,
                validoDe,
                validoAte,
                bytes,
                request.Senha
            );

            return Ok(new
            {
                mensagem = "Certificado digital armazenado com sucesso.",
                titular,
                validoAte,
                arquivoConfigurado = true
            });
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return BadRequest(new
            {
                mensagem = "Arquivo inválido ou senha incorreta."
            });
        }

    }


    private async Task SalvarCertificadoAsync(
    Guid empresaId,
    string nomeArquivo,
    string titular,
    string cnpj,
    string emissor,
    string thumbprint,
    string numeroSerie,
    DateTime validoDe,
    DateTime validoAte,
    byte[] arquivoPfx,
    string senha)
    {
        var pfxCriptografado = _criptografia.Criptografar(arquivoPfx);

        var senhaBytes = System.Text.Encoding.UTF8.GetBytes(senha);

        try
        {
            var senhaCriptografada = _criptografia.Criptografar(senhaBytes);

            await using var transacao =
                await _context.Database.BeginTransactionAsync();

            var certificado = await _context.CertificadosDigitais
                .FirstOrDefaultAsync(x => x.EmpresaId == empresaId);

            if (certificado == null)
            {
                certificado = new CertificadoDigital
                {
                    EmpresaId = empresaId,
                    CriadoEm = DateTime.UtcNow
                };

                _context.CertificadosDigitais.Add(certificado);
            }

            certificado.NomeArquivo = Path.GetFileName(nomeArquivo);
            certificado.Titular = titular;
            certificado.Cnpj = cnpj;
            certificado.Emissor = emissor;
            certificado.Thumbprint = thumbprint;
            certificado.NumeroSerie = numeroSerie;
            certificado.ValidoDe = validoDe;
            certificado.ValidoAte = validoAte;
            certificado.TipoArmazenamento = 1;
            certificado.ReferenciaArmazenamento = "POSTGRESQL_CRIPTOGRAFADO";
            certificado.Ativo = true;
            certificado.AtualizadoEm = DateTime.UtcNow;

            var arquivo = await _context.CertificadosDigitaisArquivos
                .FirstOrDefaultAsync(
                    x => x.CertificadoDigitalId == certificado.Id);

            if (arquivo == null)
            {
                arquivo = new CertificadoDigitalArquivo
                {
                    CertificadoDigitalId = certificado.Id
                };

                _context.CertificadosDigitaisArquivos.Add(arquivo);
            }

            arquivo.ConteudoCriptografado = pfxCriptografado.Conteudo;
            arquivo.Nonce = pfxCriptografado.Nonce;
            arquivo.TagAutenticacao = pfxCriptografado.Tag;

            arquivo.SenhaCriptografada = senhaCriptografada.Conteudo;
            arquivo.SenhaNonce = senhaCriptografada.Nonce;
            arquivo.SenhaTagAutenticacao = senhaCriptografada.Tag;

            await _context.SaveChangesAsync();
            await transacao.CommitAsync();
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations
                .ZeroMemory(senhaBytes);
        }
    }


    [HttpGet("meu/testar-leitura")]
    public async Task<IActionResult> TestarLeituraCertificado()
    {
        var empresaIdClaim = User.FindFirst("empresaId")?.Value;

        if (!Guid.TryParse(empresaIdClaim, out var empresaId))
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });

        var certificadoDigital = await _context.CertificadosDigitais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .Select(x => new
            {
                x.Id,
                x.Thumbprint,
                x.Ativo
            })
            .FirstOrDefaultAsync();

        if (certificadoDigital == null || !certificadoDigital.Ativo)
            return NotFound(new
            {
                mensagem = "Certificado digital ativo não encontrado."
            });

        var arquivo = await _context.CertificadosDigitaisArquivos
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.CertificadoDigitalId == certificadoDigital.Id);

        if (arquivo == null)
            return NotFound(new
            {
                mensagem = "Arquivo criptografado não encontrado."
            });

        byte[]? pfx = null;
        byte[]? senhaBytes = null;

        try
        {
            pfx = _criptografia.Descriptografar(
                arquivo.ConteudoCriptografado,
                arquivo.Nonce,
                arquivo.TagAutenticacao);

            senhaBytes = _criptografia.Descriptografar(
                arquivo.SenhaCriptografada,
                arquivo.SenhaNonce,
                arquivo.SenhaTagAutenticacao);

            var senha = System.Text.Encoding.UTF8.GetString(senhaBytes);

            using var certificado = new X509Certificate2(
                pfx,
                senha,
                X509KeyStorageFlags.EphemeralKeySet);

            var validoDe = certificado.NotBefore.ToUniversalTime();
            var validoAte = certificado.NotAfter.ToUniversalTime();
            var agora = DateTime.UtcNow;

            var cnpjCertificado = ExtrairCnpjCertificado(certificado);

            var empresaCnpj = await _context.Empresas
                .AsNoTracking()
                .Where(x => x.Id == empresaId)
                .Select(x => x.Cnpj)
                .FirstOrDefaultAsync();

            var cnpjNormalizado = new string(
                (empresaCnpj ?? "").Where(char.IsDigit).ToArray());

            var cnpjConfere =
                !string.IsNullOrEmpty(cnpjCertificado) &&
                cnpjCertificado == cnpjNormalizado;

            var resultado =
                certificado.HasPrivateKey &&
                agora >= validoDe &&
                agora <= validoAte &&
                cnpjConfere &&
                string.Equals(
                    certificado.Thumbprint,
                    certificadoDigital.Thumbprint,
                    StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                leituraRealizada = true,
                certificadoValido = resultado,
                possuiChavePrivada = certificado.HasPrivateKey,
                dentroDaValidade = agora >= validoDe && agora <= validoAte,
                cnpjConfere,
                validoAte
            });
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return UnprocessableEntity(new
            {
                mensagem = "Falha ao descriptografar ou carregar o certificado."
            });
        }
        catch (AsnContentException)
        {
            return UnprocessableEntity(new
            {
                mensagem = "Falha ao interpretar os dados de identificação do certificado."
            });
        }
        finally
        {
            if (pfx != null)
                System.Security.Cryptography.CryptographicOperations
                    .ZeroMemory(pfx);

            if (senhaBytes != null)
                System.Security.Cryptography.CryptographicOperations
                    .ZeroMemory(senhaBytes);
        }
    }


    [HttpGet("meu/testar-zeus")]
    public async Task<IActionResult> TestarCertificadoZeus()
    {
        if (!Guid.TryParse(
            User.FindFirst("empresaId")?.Value,
            out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        byte[]? pfx = null;

        try
        {
            var dados = await _certificadoDigitalService
                .ObterCertificadoAsync(empresaId);

            pfx = dados.Pfx;

            var configuracao = _zeusFactory
                .CriarComCertificadoEmMemoria(
                    pfx,
                    dados.Senha);

            var certificadoZeus = configuracao.Certificado;

            return Ok(new
            {
                mensagem = "Configuração do Zeus criada com sucesso.",
                tipoCertificado = certificadoZeus.TipoCertificado.ToString(),
                certificadoEmMemoria =
                    certificadoZeus.ArrayBytesArquivo?.Length > 0,
                possuiSenha =
                    !string.IsNullOrWhiteSpace(certificadoZeus.Senha)
            });
        }
        finally
        {
            if (pfx != null)
                CryptographicOperations.ZeroMemory(pfx);
        }
    }



    [HttpGet("meu/testar-ssl")]
    public async Task<IActionResult> TestarSsl()
    {
        if (!Guid.TryParse(
            User.FindFirst("empresaId")?.Value,
            out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        byte[]? pfx = null;

        try
        {
            var dados = await _certificadoDigitalService
                .ObterCertificadoAsync(empresaId);

            pfx = dados.Pfx;

            using var certificado = new X509Certificate2(
                pfx,
                dados.Senha,
                X509KeyStorageFlags.DefaultKeySet);

            if (!certificado.HasPrivateKey)
            {
                return UnprocessableEntity(new
                {
                    mensagem = "Certificado sem chave privada."
                });
            }

            // Mesmo host de homologação utilizado para a NFC-e GO.
            // Testa a negociação TLS, sem enviar XML fiscal.
            var url = "https://homolog.sefaz.go.gov.br/nfe/services/NFeAutorizacao4?wsdl";

            using var handler = new HttpClientHandler();

            handler.ClientCertificateOptions =
                ClientCertificateOption.Manual;

            handler.ClientCertificates.Add(certificado);

            // Mantém a validação normal do certificado do servidor.
            using var cliente = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(20)
            };

            using var resposta = await cliente.GetAsync(url);

            return Ok(new
            {
                conexaoHttpsEstabelecida = true,
                statusHttp = (int)resposta.StatusCode,
                certificadoPossuiChavePrivada = certificado.HasPrivateKey,
                mensagem = "Conexão HTTPS realizada."
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.ToString());

            return Ok(new
            {
                conexaoHttpsEstabelecida = false,
                erro = ex.GetType().Name,
                causa = ex.GetBaseException().Message
            });
        }
        finally
        {
            if (pfx != null)
                CryptographicOperations.ZeroMemory(pfx);
        }
    }


    [HttpGet("meu/testar-soap-zeus")]
    public async Task<IActionResult> TestarSoapZeus()
    {
        if (!Guid.TryParse(
            User.FindFirst("empresaId")?.Value,
            out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        byte[]? pfx = null;

        try
        {
            var dados = await _certificadoDigitalService
                .ObterCertificadoAsync(empresaId);

            pfx = dados.Pfx;

            var configuracao = _zeusConfigurationFactory
                .CriarComCertificadoEmMemoria(
                    dados.Pfx,
                    dados.Senha);

            configuracao.ModeloDocumento =
                DFe.Classes.Flags.ModeloDocumento.NFCe;

            using var servicoNFe =
                new NFe.Servicos.ServicosNFe(configuracao);

            var retorno = servicoNFe.NfeStatusServico();

            return Ok(new
            {
                sucesso = true,
                cStat = retorno.Retorno.cStat,
                motivo = retorno.Retorno.xMotivo
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "ERRO SOAP ZEUS: " + ex);

            return Ok(new
            {
                sucesso = false,
                erro = ex.GetType().FullName,
                causa = ex.GetBaseException().Message,
                detalhes = ex.ToString()
            });
        }
        finally
        {
            if (pfx != null)
                CryptographicOperations.ZeroMemory(pfx);
        }
    }



    [HttpGet("diagnostico/url-autorizacao")]
    public IActionResult ConsultarUrlAutorizacao()
    {
        var configuracao = new ConfiguracaoServico
        {
            cUF = Estado.GO,
            tpAmb = TipoAmbiente.Homologacao,
            VersaoLayout = VersaoServico.Versao400,
            ModeloDocumento = ModeloDocumento.NFCe,
            tpEmis = TipoEmissao.teNormal,
            DefineVersaoServicosAutomaticamente = true
        };

        string url = Enderecador.ObterUrlServico(
            ServicoNFe.NFeAutorizacao,
            configuracao);

        return Ok(new
        {
            Estado = "GO",
            Ambiente = "Homologacao",
            Modelo = "NFCe",
            UrlAutorizacao = url
        });
    }



    private static string? ExtrairCnpjCertificado(X509Certificate2 certificado)
    {
        var extensao = certificado.Extensions
            .FirstOrDefault(x => x.Oid?.Value == "2.5.29.17");

        if (extensao == null)
            return null;

        var reader = new AsnReader(
            extensao.RawData,
            AsnEncodingRules.DER);

        var nomes = reader.ReadSequence();

        while (nomes.HasData)
        {
            var tag = nomes.PeekTag();

            // otherName [0]
            if (tag.TagClass == TagClass.ContextSpecific &&
                tag.TagValue == 0)
            {
                var otherName = nomes.ReadSequence(
                    new Asn1Tag(TagClass.ContextSpecific, 0));

                var oid = otherName.ReadObjectIdentifier();

                if (oid == "2.16.76.1.3.3")
                {
                    var valor = otherName.ReadSequence( new Asn1Tag(TagClass.ContextSpecific, 0, true));

                    var tipo = valor.PeekTag();

                    string texto;

                    if (tipo.HasSameClassAndValue(
                        new Asn1Tag(UniversalTagNumber.UTF8String)))
                    {
                        texto = valor.ReadCharacterString(
                            UniversalTagNumber.UTF8String);
                    }
                    else if (tipo.HasSameClassAndValue(
                        new Asn1Tag(UniversalTagNumber.IA5String)))
                    {
                        texto = valor.ReadCharacterString(
                            UniversalTagNumber.IA5String);
                    }
                    else if (tipo.HasSameClassAndValue(
                        new Asn1Tag(UniversalTagNumber.PrintableString)))
                    {
                        texto = valor.ReadCharacterString(
                            UniversalTagNumber.PrintableString);
                    }
                    else if (tipo.HasSameClassAndValue(
                        new Asn1Tag(UniversalTagNumber.OctetString)))
                    {
                        texto = System.Text.Encoding.ASCII.GetString(
                            valor.ReadOctetString());
                    }
                    else
                    {
                        return null;
                    }

                    if (valor.HasData)
                        return null;

                    var numeros = new string( texto.Where(char.IsDigit).ToArray());

                    if (numeros.Length == 14 &&
                        !valor.HasData &&
                        !otherName.HasData)
                    {
                        return numeros;
                    }

                    return null;
                }
            }
            else
            {
                nomes.ReadEncodedValue();
            }
        }

        return null;
    }


    [HttpGet("meu/comparar-certificado-xml")]
    public async Task<IActionResult> CompararCertificadoXml()
    {
        if (!Guid.TryParse(
            User.FindFirst("empresaId")?.Value,
            out var empresaId))
        {
            return Unauthorized();
        }

        var nota = await _context.NotasFiscais
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Modelo == 65 &&
                x.Ambiente == 2 &&
                x.Numero == 900063)
            .Select(x => x.XmlEnvio)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(nota))
            return NotFound(new
            {
                mensagem = "XML da NFC-e não encontrado."
            });

        var xml = new System.Xml.XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };

        xml.LoadXml(nota);

        var namespaceManager =
            new System.Xml.XmlNamespaceManager(xml.NameTable);

        namespaceManager.AddNamespace(
            "ds",
            System.Security.Cryptography.Xml.SignedXml.XmlDsigNamespaceUrl);

        var elementoCertificado = xml.SelectSingleNode(
            "//ds:Signature/ds:KeyInfo/ds:X509Data/ds:X509Certificate",
            namespaceManager);

        if (elementoCertificado == null)
            return BadRequest(new
            {
                mensagem = "Certificado público não encontrado no XML."
            });

        var bytesCertificado = Convert.FromBase64String(
            elementoCertificado.InnerText);

        using var certificadoXml =
            new X509Certificate2(bytesCertificado);

        var certificadoAiven = await _context.CertificadosDigitais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId)
            .Select(x => x.Thumbprint)
            .FirstOrDefaultAsync();

        var confere = string.Equals(
            certificadoXml.Thumbprint,
            certificadoAiven,
            StringComparison.OrdinalIgnoreCase);

        return Ok(new
        {
            numero = 900063,
            certificadoConfere = confere
        });
    }


    [HttpGet("meu/validar-assinatura-xml")]
    public async Task<IActionResult> ValidarAssinaturaXml()
    {
        if (!Guid.TryParse(
            User.FindFirst("empresaId")?.Value,
            out var empresaId))
        {
            return Unauthorized();
        }

        var xmlNota = await _context.NotasFiscais
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Modelo == 65 &&
                x.Ambiente == 2 &&
                x.Numero == 900063)
            .Select(x => x.XmlEnvio)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(xmlNota))
            return NotFound(new
            {
                mensagem = "XML da NFC-e não encontrado."
            });

        var xml = new XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };

        xml.LoadXml(xmlNota);

        var namespaceManager = new XmlNamespaceManager(xml.NameTable);
        namespaceManager.AddNamespace(
            "ds",
            SignedXml.XmlDsigNamespaceUrl);

        var assinatura = xml.SelectSingleNode(
            "//ds:Signature",
            namespaceManager) as XmlElement;

        if (assinatura == null)
            return BadRequest(new
            {
                mensagem = "Assinatura digital não encontrada."
            });

        var certificadoNode = assinatura.SelectSingleNode(
            "ds:KeyInfo/ds:X509Data/ds:X509Certificate",
            namespaceManager);

        if (certificadoNode == null)
            return BadRequest(new
            {
                mensagem = "Certificado público não encontrado no XML."
            });

        using var certificadoXml = new X509Certificate2(
            Convert.FromBase64String(certificadoNode.InnerText));

        var thumbprintAiven = await _context.CertificadosDigitais
            .AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Ativo)
            .Select(x => x.Thumbprint)
            .FirstOrDefaultAsync();

        var certificadoConfere = string.Equals(
            certificadoXml.Thumbprint,
            thumbprintAiven,
            StringComparison.OrdinalIgnoreCase);

        // Em NF-e, a assinatura referencia infNFe pelo atributo Id.
        // A resolução desse Id deve ser controlada explicitamente.
        var signedXml = new SignedXmlNFe(xml);
        signedXml.LoadXml(assinatura);

        var assinaturaValida = signedXml.CheckSignature(
            certificadoXml,
            true);

        return Ok(new
        {
            numero = 900063,
            certificadoConfere,
            assinaturaValida
        });
    }



    [HttpDelete("meu")]
    public async Task<IActionResult> RemoverMeuCertificado()
    {
        var empresaIdClaim = User.FindFirst("empresaId")?.Value;

        if (!Guid.TryParse(empresaIdClaim, out var empresaId))
        {
            return Unauthorized(new
            {
                mensagem = "Empresa não identificada no token."
            });
        }

        var certificado = await _context.CertificadosDigitais
            .FirstOrDefaultAsync(x => x.EmpresaId == empresaId);

        if (certificado == null)
        {
            return NotFound(new
            {
                mensagem = "Nenhum certificado cadastrado para esta empresa."
            });
        }

        await using var transacao =
            await _context.Database.BeginTransactionAsync();

        var arquivos = await _context.CertificadosDigitaisArquivos
            .Where(x => x.CertificadoDigitalId == certificado.Id)
            .ToListAsync();

        _context.CertificadosDigitaisArquivos.RemoveRange(arquivos);

        _context.CertificadosDigitais.Remove(certificado);

        await _context.SaveChangesAsync();
        await transacao.CommitAsync();

        return Ok(new
        {
            mensagem = "Certificado digital removido com sucesso."
        });
    }




}

public class UploadCertificadoDigitalRequest
{
    public IFormFile Arquivo { get; set; } = null!;

    public string Senha { get; set; } = string.Empty;
}