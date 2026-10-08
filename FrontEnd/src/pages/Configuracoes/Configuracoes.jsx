import { useEffect, useState } from "react";
import { obterMinhaConfiguracaoFiscal, atualizarMinhaConfiguracaoFiscal, } from "../../services/configuracaoFiscalService";
import { obterConfiguracoesTributarias, cadastrarConfiguracaoTributaria, atualizarConfiguracaoTributaria, desativarConfiguracaoTributaria} from "../../services/configuracaoTributariaService";
import ConfiguracaoTributariaModal from "./ConfiguracaoTributariaModal";
import FiscalAgent from "./FiscalAgent";
import CertificadoDigital from "./CertificadoDigital";
import "./Configuracoes.css";

const formularioInicial = {
  ambiente: 2,
  serieNFe: 1,
  serieNFCe: 1,
  proximoNumeroNFe: 1,
  proximoNumeroNFCe: 1,
  cscId: "",
  csc: "",
};

export default function Configuracoes() {
    const [form, setForm] = useState(formularioInicial);
    const [cscConfigurado, setCscConfigurado] = useState(false);
    const [mostrarCsc, setMostrarCsc] = useState(false);
    const [carregando, setCarregando] = useState(true);
    const [salvando, setSalvando] = useState(false);
    const [erro, setErro] = useState("");
    const [sucesso, setSucesso] = useState("");
    const [configuracoesTributarias, setConfiguracoesTributarias] = useState([]);
    const [carregandoTributarias, setCarregandoTributarias] = useState(true);
    const [modalTributarioAberto, setModalTributarioAberto] = useState(false);
    const [configuracaoTributariaSelecionada, setConfiguracaoTributariaSelecionada, ] = useState(null);
    const [salvandoTributaria, setSalvandoTributaria] = useState(false);
    const [configuracaoParaDesativar, setConfiguracaoParaDesativar, ] = useState(null);
    const [erroDesativacao, setErroDesativacao] = useState("");

    useEffect(() => {
        carregarConfiguracao();
        carregarConfiguracoesTributarias();
    }, []);

    async function carregarConfiguracao() {
        try {
        setCarregando(true);
        setErro("");

        const dados = await obterMinhaConfiguracaoFiscal();

        setForm({
            ambiente: dados.ambiente ?? 2,
            serieNFe: dados.serieNFe ?? 1,
            serieNFCe: dados.serieNFCe ?? 1,
            proximoNumeroNFe: dados.proximoNumeroNFe ?? 1,
            proximoNumeroNFCe: dados.proximoNumeroNFCe ?? 1,
            cscId: dados.cscId || "",

            // O backend não devolve o CSC.
            csc: "",
        });

        setCscConfigurado(
            Boolean(dados.cscConfigurado)
        );
        } catch (error) {
        console.error(
            "Erro ao carregar configuração fiscal:",
            error
        );

        setErro(
            error.message ||
            "Não foi possível carregar a configuração fiscal."
        );
        } finally {
        setCarregando(false);
        }
    }

    async function carregarConfiguracoesTributarias() {
    try {
        setCarregandoTributarias(true);

        const dados =
        await obterConfiguracoesTributarias();

        setConfiguracoesTributarias(
        Array.isArray(dados) ? dados : []
        );
    } catch (error) {
        console.error(
        "Erro ao carregar configurações tributárias:",
        error
        );

        setErro(
        error.message ||
            "Não foi possível carregar as configurações tributárias."
        );
    } finally {
        setCarregandoTributarias(false);
    }
    }

    function abrirNovaConfiguracaoTributaria() {
    setConfiguracaoTributariaSelecionada(null);
    setModalTributarioAberto(true);
    }

    function abrirEdicaoConfiguracaoTributaria(configuracao) {
        setConfiguracaoTributariaSelecionada(configuracao);
        setModalTributarioAberto(true);
    }

    function fecharModalTributario() {
    if (salvandoTributaria) {
        return;
    }

    setModalTributarioAberto(false);
    setConfiguracaoTributariaSelecionada(null);
    }

    async function salvarConfiguracaoTributaria(request) {
    try {
        setSalvandoTributaria(true);
        setErro("");
        setSucesso("");

        if (configuracaoTributariaSelecionada?.id) {
        await atualizarConfiguracaoTributaria(
            configuracaoTributariaSelecionada.id,
            request
        );

        setSucesso(
            "Configuração tributária atualizada com sucesso."
        );
        } else {
        await cadastrarConfiguracaoTributaria(request);

        setSucesso(
            "Configuração tributária cadastrada com sucesso."
        );
        }

        setModalTributarioAberto(false);
        setConfiguracaoTributariaSelecionada(null);

        await carregarConfiguracoesTributarias();
    } catch (error) {
        console.error(
        "Erro ao salvar configuração tributária:",
        error
        );

        setErro(
        error.message ||
            "Não foi possível salvar a configuração tributária."
        );
    } finally {
        setSalvandoTributaria(false);
    }
    }

    async function confirmarDesativacao() {
    if (!configuracaoParaDesativar) {
        return;
    }

    try {
        setErroDesativacao("");

        await desativarConfiguracaoTributaria(
        configuracaoParaDesativar.id
        );

        setSucesso(
        "Configuração tributária desativada com sucesso."
        );

        setConfiguracaoParaDesativar(null);
        setErroDesativacao("");

        await carregarConfiguracoesTributarias();
    } catch (error) {
        console.error(
        "Erro ao desativar configuração tributária:",
        error
        );

        setErroDesativacao(
        error.message ||
            "Não foi possível desativar a configuração tributária."
        );
    }
    }

    function alterarCampo(event) {
        const { name, value } = event.target;

        setForm((anterior) => ({
        ...anterior,
        [name]: value,
        }));

        setSucesso("");
    }

    async function salvar(event) {
        event.preventDefault();

        try {
        setSalvando(true);
        setErro("");
        setSucesso("");

        const request = {
            ambiente: Number(form.ambiente),

            serieNFe: Number(form.serieNFe),
            serieNFCe: Number(form.serieNFCe),

            proximoNumeroNFe:
            Number(form.proximoNumeroNFe),

            proximoNumeroNFCe:
            Number(form.proximoNumeroNFCe),

            cscId:
            form.cscId.trim() || null,

            /*
            * Campo vazio significa:
            * manter o CSC atual.
            */
            csc:
            form.csc.trim() || null,
        };

        const dados =
            await atualizarMinhaConfiguracaoFiscal(
            request
            );

        setForm((anterior) => ({
            ...anterior,

            ambiente:
            dados.ambiente ?? anterior.ambiente,

            serieNFe:
            dados.serieNFe ?? anterior.serieNFe,

            serieNFCe:
            dados.serieNFCe ?? anterior.serieNFCe,

            proximoNumeroNFe:
            dados.proximoNumeroNFe ??
            anterior.proximoNumeroNFe,

            proximoNumeroNFCe:
            dados.proximoNumeroNFCe ??
            anterior.proximoNumeroNFCe,

            cscId:
            dados.cscId ?? "",

            // Nunca mantemos o CSC digitado na tela.
            csc: "",
        }));

        setCscConfigurado(
            Boolean(dados.cscConfigurado)
        );

        setMostrarCsc(false);

        setSucesso(
            "Configurações fiscais atualizadas com sucesso."
        );
        } catch (error) {
        console.error(
            "Erro ao salvar configuração fiscal:",
            error
        );

        setErro(
            error.message ||
            "Não foi possível salvar as configurações fiscais."
        );
        } finally {
        setSalvando(false);
        }
    }

    if (carregando) {
        return (
        <div className="configuracoes-pagina">
            <div className="configuracoes-carregando">
            <i className="bi bi-arrow-repeat" />
            Carregando configurações...
            </div>
        </div>
        );
    }

    return (
        <div className="configuracoes-pagina">
        <div className="configuracoes-cabecalho">
            <div>
            <h1>Configurações</h1>

            <p>
                Parâmetros utilizados na emissão dos
                documentos fiscais.
            </p>
            </div>
        </div>

        {erro && (
            <div className="configuracoes-mensagem configuracoes-mensagem-erro">
            <i className="bi bi-exclamation-circle" />
            {erro}
            </div>
        )}

        {sucesso && (
            <div className="configuracoes-mensagem configuracoes-mensagem-sucesso">
            <i className="bi bi-check-circle" />
            {sucesso}
            </div>
        )}

        <form onSubmit={salvar}>
            {/* NFC-e */}

            <section className="configuracoes-card">
            <div className="configuracoes-card-titulo">
                <div className="configuracoes-card-icone">
                <i className="bi bi-receipt" />
                </div>

                <div>
                <h2>NFC-e</h2>
                <p>
                    Parâmetros da Nota Fiscal de
                    Consumidor Eletrônica.
                </p>
                </div>
            </div>

            <div className="configuracoes-grid">
                <div className="configuracoes-campo">
                <label>Ambiente</label>

                <select
                    name="ambiente"
                    value={form.ambiente}
                    onChange={alterarCampo}
                >
                    <option value="2">
                    Homologação
                    </option>

                    <option value="1">
                    Produção
                    </option>
                </select>
                </div>

                <div className="configuracoes-campo">
                <label>Série NFC-e</label>

                <input
                    type="number"
                    min="1"
                    name="serieNFCe"
                    value={form.serieNFCe}
                    onChange={alterarCampo}
                />
                </div>

                <div className="configuracoes-campo">
                <label>Próximo número NFC-e</label>

                <input
                    type="number"
                    min="1"
                    name="proximoNumeroNFCe"
                    value={form.proximoNumeroNFCe}
                    onChange={alterarCampo}
                />
                </div>
            </div>
            </section>

            {/* NF-e */}

            <section className="configuracoes-card">
            <div className="configuracoes-card-titulo">
                <div className="configuracoes-card-icone">
                <i className="bi bi-file-earmark-text" />
                </div>

                <div>
                <h2>NF-e</h2>

                <p>
                    Parâmetros reservados para emissão
                    de NF-e modelo 55.
                </p>
                </div>
            </div>

            <div className="configuracoes-grid">
                <div className="configuracoes-campo">
                <label>Série NF-e</label>

                <input
                    type="number"
                    min="1"
                    name="serieNFe"
                    value={form.serieNFe}
                    onChange={alterarCampo}
                />
                </div>

                <div className="configuracoes-campo">
                <label>Próximo número NF-e</label>

                <input
                    type="number"
                    min="1"
                    name="proximoNumeroNFe"
                    value={form.proximoNumeroNFe}
                    onChange={alterarCampo}
                />
                </div>
            </div>
            </section>

            {/* CSC */}

            <section className="configuracoes-card">
            <div className="configuracoes-card-titulo">
                <div className="configuracoes-card-icone">
                <i className="bi bi-qr-code" />
                </div>

                <div>
                <h2>CSC / QR Code</h2>

                <p>
                    Código de Segurança do Contribuinte
                    utilizado no QR Code da NFC-e.
                </p>
                </div>
            </div>

            <div className="configuracoes-status-csc">
                <span>CSC</span>

                {cscConfigurado ? (
                <strong className="configuracoes-status-ok">
                    <i className="bi bi-check-circle" />
                    Configurado
                </strong>
                ) : (
                <strong className="configuracoes-status-pendente">
                    <i className="bi bi-exclamation-circle" />
                    Não configurado
                </strong>
                )}
            </div>

            <div className="configuracoes-grid configuracoes-grid-csc">
                <div className="configuracoes-campo">
                <label>CSC ID</label>

                <input
                    name="cscId"
                    value={form.cscId}
                    onChange={alterarCampo}
                    placeholder="Identificador do CSC"
                />
                </div>

                <div className="configuracoes-campo configuracoes-coluna-2">
                <label>
                    {cscConfigurado
                    ? "Novo CSC / Token"
                    : "CSC / Token"}
                </label>

                <div className="configuracoes-csc-input">
                    <input
                    type={
                        mostrarCsc
                        ? "text"
                        : "password"
                    }
                    name="csc"
                    value={form.csc}
                    onChange={alterarCampo}
                    autoComplete="new-password"
                    placeholder={
                        cscConfigurado
                        ? "Deixe vazio para manter o CSC atual"
                        : "Informe o CSC"
                    }
                    />

                    <button
                    type="button"
                    onClick={() =>
                        setMostrarCsc(
                        (anterior) => !anterior
                        )
                    }
                    title={
                        mostrarCsc
                        ? "Ocultar CSC"
                        : "Mostrar CSC"
                    }
                    >
                    <i
                        className={
                        mostrarCsc
                            ? "bi bi-eye-slash"
                            : "bi bi-eye"
                        }
                    />
                    </button>
                </div>

                {cscConfigurado && (
                    <small>
                    Por segurança, o CSC atual não é
                    exibido. Preencha somente para
                    substituí-lo.
                    </small>
                )}
                </div>
            </div>
            </section>

            <div className="configuracoes-acoes">
            <button
                type="submit"
                className="configuracoes-salvar"
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

        {/* FISCAL.AGENT */}
        <FiscalAgent />

        {/* CERTIFICADO DIGITAL */}
        <CertificadoDigital />


        {/* CONFIGURAÇÕES TRIBUTÁRIAS */}

        <section className="configuracoes-card">
        <div className="configuracoes-card-titulo configuracoes-tributarias-titulo">
            <div className="configuracoes-card-titulo-info">
            <div className="configuracoes-card-icone">
                <i className="bi bi-percent" />
            </div>

            <div>
                <h2>Configurações Tributárias</h2>

                <p>
                Regras tributárias utilizadas no cadastro
                dos produtos.
                </p>
            </div>
            </div>

            <button
            type="button"
            className="configuracoes-nova-tributacao"
            onClick={abrirNovaConfiguracaoTributaria}
            >
            <i className="bi bi-plus-lg" />
            Nova configuração
            </button>
        </div>

        {carregandoTributarias ? (
            <div className="configuracoes-tributarias-carregando">
            <i className="bi bi-arrow-repeat" />
            Carregando configurações tributárias...
            </div>
        ) : configuracoesTributarias.length === 0 ? (
            <div className="configuracoes-tributarias-vazio">
            Nenhuma configuração tributária cadastrada.
            </div>
        ) : (
            <div className="configuracoes-tabela-container">
            <table className="configuracoes-tabela">
                <thead>
                <tr>
                    <th>Nome</th>
                    <th>CFOP</th>
                    <th>CST ICMS</th>
                    <th>CSOSN</th>
                    <th>Alíquota ICMS</th>
                    <th className="configuracoes-tabela-acoes">
                    Ações
                    </th>
                </tr>
                </thead>

                <tbody>
                {configuracoesTributarias.map(
                    (configuracao) => (
                    <tr key={configuracao.id}>
                        <td>
                        <strong>
                            {configuracao.nome}
                        </strong>
                        </td>

                        <td>{configuracao.cfop || "-"}</td>

                        <td>
                        {configuracao.cstIcms || "-"}
                        </td>

                        <td>
                        {configuracao.csosn || "-"}
                        </td>

                        <td>
                        {configuracao.aliquotaIcms != null
                            ? `${Number(
                                configuracao.aliquotaIcms
                            ).toLocaleString("pt-BR", {
                                minimumFractionDigits: 2,
                                maximumFractionDigits: 2,
                            })}%`
                            : "-"}
                        </td>

                    <td className="configuracoes-tabela-acoes">
                    <div className="configuracoes-botoes-acoes">
                        <button
                        type="button"
                        className="configuracoes-editar"
                        title="Editar configuração"
                        onClick={() =>
                            abrirEdicaoConfiguracaoTributaria(configuracao)
                        }
                        >
                        <i className="bi bi-pencil" />
                        </button>

                        <button
                        type="button"
                        className="configuracoes-desativar"
                        title="Desativar configuração"
                        onClick={() =>
                            setConfiguracaoParaDesativar(configuracao)
                        }
                        >
                        <i className="bi bi-trash" />
                        </button>
                    </div>
                    </td>
                    </tr>
                    )
                )}
                </tbody>
            </table>
            </div>
        )}
        </section>

        <ConfiguracaoTributariaModal
            aberto={modalTributarioAberto}
            configuracao={configuracaoTributariaSelecionada}
            salvando={salvandoTributaria}
            onFechar={() => setModalTributarioAberto(false)}
            onSalvar={salvarConfiguracaoTributaria}
        />

        {configuracaoParaDesativar && (
        <div
            className="configuracoes-modal-overlay"
            onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
                setErroDesativacao("");
                setConfiguracaoParaDesativar(null);
            }
            }}
        >
            <div className="configuracoes-confirmacao">
            <div className="configuracoes-confirmacao-icone">
                <i className="bi bi-exclamation-triangle" />
            </div>

            <h2>Desativar configuração?</h2>

            <p>
                Deseja realmente desativar a configuração
                <strong>
                {" "}
                {configuracaoParaDesativar.nome}
                </strong>
                ?
            </p>

            <p className="configuracoes-confirmacao-aviso">
                Se ela estiver sendo utilizada por algum produto ativo,
                a operação será bloqueada.
            </p>

            {erroDesativacao && (
            <div className="configuracoes-confirmacao-erro">
                <i className="bi bi-exclamation-circle" />
                <span>{erroDesativacao}</span>
            </div>
            )}

            <div className="configuracoes-confirmacao-acoes">
                <button
                type="button"
                className="configuracoes-modal-cancelar"
                onClick={() => {
                setErroDesativacao("");
                setConfiguracaoParaDesativar(null);
                }}
                >
                Cancelar
                </button>

                <button
                type="button"
                className="configuracoes-confirmacao-desativar"
                onClick={confirmarDesativacao}
                >
                <i className="bi bi-trash" />
                Desativar
                </button>
            </div>
            </div>
        </div>
        )}

        </div>
    );
}