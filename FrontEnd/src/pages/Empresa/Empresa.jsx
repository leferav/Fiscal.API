import { useEffect, useState } from "react";
import { obterMinhaEmpresa, atualizarMinhaEmpresa, } from "../../services/empresaService";
import { consultarCep } from "../../services/cepService";
import "./Empresa.css";

const formularioInicial = {
  cnpj: "",
  razaoSocial: "",
  nomeFantasia: "",
  inscricaoEstadual: "",
  crt: 1,

  cep: "",
  logradouro: "",
  numero: "",
  bairro: "",
  municipio: "",
  codigoMunicipio: "",
  uf: "",
};

function somenteNumeros(valor) {
  return String(valor || "").replace(/\D/g, "");
}

function formatarCnpj(valor) {
  const numeros = somenteNumeros(valor).slice(0, 14);

  return numeros
    .replace(/^(\d{2})(\d)/, "$1.$2")
    .replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3")
    .replace(/\.(\d{3})(\d)/, ".$1/$2")
    .replace(/(\d{4})(\d)/, "$1-$2");
}

function formatarCep(valor) {
  const numeros = somenteNumeros(valor).slice(0, 8);

  return numeros.replace(/^(\d{5})(\d)/, "$1-$2");
}

export default function Empresa() {
    const [form, setForm] = useState(formularioInicial);
    const [configuracaoFiscal, setConfiguracaoFiscal] = useState(null);
    const [carregando, setCarregando] = useState(true);
    const [salvando, setSalvando] = useState(false);
    const [consultandoCep, setConsultandoCep] = useState(false);
    const [erro, setErro] = useState("");
    const [sucesso, setSucesso] = useState("");

    useEffect(() => {
        carregarEmpresa();
    }, []);

    async function carregarEmpresa() {
        try {
        setCarregando(true);
        setErro("");

        const dados = await obterMinhaEmpresa();

        setForm({
            cnpj: formatarCnpj(dados.cnpj),
            razaoSocial: dados.razaoSocial || "",
            nomeFantasia: dados.nomeFantasia || "",
            inscricaoEstadual: dados.inscricaoEstadual || "",
            crt: dados.crt ?? 1,

            cep: formatarCep(dados.cep),
            logradouro: dados.logradouro || "",
            numero: dados.numero || "",
            bairro: dados.bairro || "",
            municipio: dados.municipio || "",
            codigoMunicipio: dados.codigoMunicipio ?? "",
            uf: dados.uf || "",
        });

        setConfiguracaoFiscal(
            dados.configuracaoFiscal || null
        );
        } catch (error) {
        console.error(
            "Erro ao carregar empresa:",
            error
        );

        setErro(
            error.message ||
            "Não foi possível carregar os dados da empresa."
        );
        } finally {
        setCarregando(false);
        }
    }

    function alterarCampo(event) {
    const { name, value } = event.target;

    let novoValor = value;

    if (name === "cnpj") {
        novoValor = formatarCnpj(value);
    }

    if (name === "cep") {
        novoValor = formatarCep(value);
    }

    if (name === "uf") {
        novoValor = value
        .replace(/[^a-zA-Z]/g, "")
        .toUpperCase()
        .slice(0, 2);
    }

    setForm((anterior) => ({
        ...anterior,
        [name]: novoValor,
    }));

    setSucesso("");
    }

    async function buscarCep() {
    const cepNumerico = String(form.cep || "")
        .replace(/\D/g, "");

    if (cepNumerico.length !== 8) {
        return;
    }

    try {
        setConsultandoCep(true);
        setErro("");

        const dados = await consultarCep(cepNumerico);

        setForm((anterior) => ({
        ...anterior,
        cep: formatarCep(dados.cep),
        logradouro: dados.logradouro || "",
        numero: "",
        bairro: dados.bairro || "",
        municipio: dados.municipio || "",
        uf: dados.uf || "",
        codigoMunicipio: dados.codigoMunicipio || "",
        }));
    } catch (error) {
        console.error("Erro ao consultar CEP:", error);

        setErro(
        error.message ||
            "Não foi possível consultar o CEP."
        );
    } finally {
        setConsultandoCep(false);
    }
    }

    async function salvar(event) {
        event.preventDefault();

        try {
        setSalvando(true);
        setErro("");
        setSucesso("");

        const request = {
            ...form,
            crt: Number(form.crt),
            codigoMunicipio: Number(
            form.codigoMunicipio
            ),
        };

        const dadosAtualizados =
            await atualizarMinhaEmpresa(request);

        setForm((anterior) => ({
            ...anterior,
            cnpj:
            dadosAtualizados.cnpj ??
            anterior.cnpj,
            razaoSocial:
            dadosAtualizados.razaoSocial ??
            anterior.razaoSocial,
            nomeFantasia:
            dadosAtualizados.nomeFantasia ??
            "",
            inscricaoEstadual:
            dadosAtualizados.inscricaoEstadual ??
            "",
            crt:
            dadosAtualizados.crt ??
            anterior.crt,
            cep:
            dadosAtualizados.cep ??
            anterior.cep,
            logradouro:
            dadosAtualizados.logradouro ??
            anterior.logradouro,
            numero:
            dadosAtualizados.numero ??
            anterior.numero,
            bairro:
            dadosAtualizados.bairro ??
            anterior.bairro,
            municipio:
            dadosAtualizados.municipio ??
            anterior.municipio,
            codigoMunicipio:
            dadosAtualizados.codigoMunicipio ??
            anterior.codigoMunicipio,
            uf:
            dadosAtualizados.uf ??
            anterior.uf,
        }));

        setSucesso(
            "Dados da empresa atualizados com sucesso."
        );
        } catch (error) {
        console.error(
            "Erro ao atualizar empresa:",
            error
        );

        setErro(
            error.message ||
            "Não foi possível atualizar os dados da empresa."
        );
        } finally {
        setSalvando(false);
        }
    }

    if (carregando) {
        return (
        <div className="empresa-pagina">
            <div className="empresa-carregando">
            <i className="bi bi-arrow-repeat" />
            Carregando dados da empresa...
            </div>
        </div>
        );
    }

    return (
        <div className="empresa-pagina">
        <div className="empresa-cabecalho">
            <div>
            <h1>Empresa</h1>

            <p>
                Dados do emitente utilizados na emissão
                de documentos fiscais.
            </p>
            </div>
        </div>

        {erro && (
            <div className="empresa-mensagem empresa-mensagem-erro">
            <i className="bi bi-exclamation-circle" />
            {erro}
            </div>
        )}

        {sucesso && (
            <div className="empresa-mensagem empresa-mensagem-sucesso">
            <i className="bi bi-check-circle" />
            {sucesso}
            </div>
        )}

        <form onSubmit={salvar}>
            {/* DADOS CADASTRAIS */}

            <section className="empresa-card">
            <div className="empresa-card-titulo">
                <div className="empresa-card-icone">
                <i className="bi bi-building" />
                </div>

                <div>
                <h2>Dados cadastrais</h2>
                <p>
                    Identificação da empresa emitente.
                </p>
                </div>
            </div>

            <div className="empresa-grid">
                <div className="empresa-campo">
                <label>CNPJ</label>

                <input
                    name="cnpj"
                    value={form.cnpj}
                    onChange={alterarCampo}
                    required
                />
                </div>

                <div className="empresa-campo empresa-coluna-2">
                <label>Razão Social</label>

                <input
                    name="razaoSocial"
                    value={form.razaoSocial}
                    onChange={alterarCampo}
                    required
                />
                </div>

                <div className="empresa-campo empresa-coluna-2">
                <label>Nome Fantasia</label>

                <input
                    name="nomeFantasia"
                    value={form.nomeFantasia}
                    onChange={alterarCampo}
                />
                </div>

                <div className="empresa-campo">
                <label>Inscrição Estadual</label>

                <input
                    name="inscricaoEstadual"
                    value={form.inscricaoEstadual}
                    onChange={alterarCampo}
                />
                </div>

                <div className="empresa-campo">
                <label>CRT</label>

                <select
                    name="crt"
                    value={form.crt}
                    onChange={alterarCampo}
                >
                    <option value="1">
                    1 - Simples Nacional
                    </option>

                    <option value="2">
                    2 - Simples Nacional - excesso
                    de sublimite
                    </option>

                    <option value="3">
                    3 - Regime Normal
                    </option>

                    <option value="4">
                    4 - MEI
                    </option>
                </select>
                </div>
            </div>
            </section>

            {/* ENDEREÇO */}

            <section className="empresa-card">
            <div className="empresa-card-titulo">
                <div className="empresa-card-icone">
                <i className="bi bi-geo-alt" />
                </div>

                <div>
                <h2>Endereço</h2>
                <p>
                    Endereço fiscal do estabelecimento
                    emitente.
                </p>
                </div>
            </div>

            <div className="empresa-grid">
                <div className="empresa-campo">
                <label>CEP</label>

                <input
                name="cep"
                value={form.cep}
                onChange={alterarCampo}
                onBlur={buscarCep}
                onKeyDown={(event) => {
                    if (event.key === "Enter") {
                    event.preventDefault();
                    buscarCep();
                    }
                }}
                maxLength={9}
                placeholder="00000-000"
                />

                {consultandoCep && (
                <small className="empresa-consultando">
                    <i className="bi bi-arrow-repeat" />
                    Consultando CEP...
                </small>
                )}
                </div>

                <div className="empresa-campo empresa-coluna-2">
                <label>Logradouro</label>

                <input
                    name="logradouro"
                    value={form.logradouro}
                    onChange={alterarCampo}
                />
                </div>

                <div className="empresa-campo">
                <label>Número</label>

                <input
                    name="numero"
                    value={form.numero}
                    onChange={alterarCampo}
                />
                </div>

                <div className="empresa-campo">
                <label>Bairro</label>

                <input
                    name="bairro"
                    value={form.bairro}
                    onChange={alterarCampo}
                />
                </div>

                <div className="empresa-campo empresa-coluna-2">
                <label>Município</label>

                <input
                    name="municipio"
                    value={form.municipio}
                    onChange={alterarCampo}
                />
                </div>

                <div className="empresa-campo">
                <label>Código IBGE</label>

                <input
                    type="number"
                    name="codigoMunicipio"
                    value={form.codigoMunicipio}
                    onChange={alterarCampo}
                />
                </div>

                <div className="empresa-campo">
                <label>UF</label>

                <input
                    name="uf"
                    value={form.uf}
                    onChange={alterarCampo}
                    maxLength={2}
                />
                </div>
            </div>
            </section>

            {/* CONFIGURAÇÃO FISCAL */}

            <section className="empresa-card">
            <div className="empresa-card-titulo">
                <div className="empresa-card-icone">
                <i className="bi bi-receipt" />
                </div>

                <div>
                <h2>Configuração fiscal</h2>

                <p>
                    Informações atuais utilizadas na
                    emissão da NFC-e.
                </p>
                </div>
            </div>

            {configuracaoFiscal ? (
                <div className="empresa-fiscal-grid">
                <div className="empresa-fiscal-item">
                    <span>Ambiente</span>

                    <strong>
                    {Number(
                        configuracaoFiscal.ambiente
                    ) === 1
                        ? "Produção"
                        : "Homologação"}
                    </strong>
                </div>

                <div className="empresa-fiscal-item">
                    <span>Série NFC-e</span>

                    <strong>
                    {configuracaoFiscal.serieNFCe}
                    </strong>
                </div>

                <div className="empresa-fiscal-item">
                    <span>Próximo número</span>

                    <strong>
                    {
                        configuracaoFiscal.proximoNumeroNFCe
                    }
                    </strong>
                </div>
                </div>
            ) : (
                <div className="empresa-sem-configuracao">
                Nenhuma configuração fiscal encontrada.
                </div>
            )}
            </section>

            {/* BOTÃO SALVAR */}

            <div className="empresa-acoes">
            <button
                type="submit"
                className="empresa-salvar"
                disabled={salvando}
            >
                {salvando ? (
                <>
                    <i className="bi bi-arrow-repeat" />
                    Salvando...
                </>
                ) : (
                <>
                    <i className="bi bi-check-circle" />
                    Salvar alterações
                </>
                )}
            </button>
            </div>
        </form>
        </div>
    );
}