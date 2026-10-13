import { useEffect, useMemo, useRef, useState } from "react";
import { obterProdutos } from "../../services/produtoService";
import FinalizarVendaModal from "./FinalizarVendaModal";
import { autorizarNFCeAgent, consultarSolicitacaoNFCe } from "../../services/fiscalApi";
import { obterEmpresaId } from "../../services/authService";
import "./Vendas.css";

export default function Vendas() {
    const [produtos, setProdutos] = useState([]);
    const [pesquisa, setPesquisa] = useState("");
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState("");
    const [itens, setItens] = useState([]);
    const [modalFinalizarAberto, setModalFinalizarAberto] = useState(false);
    const [emitindo, setEmitindo] = useState(false);
    const [erroEmissao, setErroEmissao] = useState("");
    const empresaId = obterEmpresaId();
    const chavePendente = empresaId ? `fiscal-pdv-pendente-${empresaId}` : null;
    const [solicitacaoPendente, setSolicitacaoPendente] = useState(() => {
        if (!empresaId) return null;
        try { return JSON.parse(localStorage.getItem(`fiscal-pdv-pendente-${empresaId}`) || "null"); }
        catch { return null; }
    });
    const envioEmCursoRef = useRef(false);
    const [mensagemEmissao, setMensagemEmissao] = useState("");
    const [esperaLonga, setEsperaLonga] = useState(false);
    const [consultaManual, setConsultaManual] = useState(0);
    const [consultandoAgora, setConsultandoAgora] = useState(false);

    // Ao recarregar a página, restaura o carrinho associado à solicitação pendente.
    useEffect(() => {
        if (solicitacaoPendente?.itens?.length) setItens(solicitacaoPendente.itens);
    }, []);

    useEffect(() => {
        if (!solicitacaoPendente) { setEsperaLonga(false); return; }
        const inicio = solicitacaoPendente.criadaEm || Date.now();
        const atualizar = () => setEsperaLonga(Date.now() - inicio >= 30000);
        atualizar();
        const intervalo = setInterval(atualizar, 1000);
        return () => clearInterval(intervalo);
    }, [solicitacaoPendente?.solicitacaoId]);

    function guardarSolicitacao(pendente) {
        if (chavePendente) {
            if (pendente) localStorage.setItem(chavePendente, JSON.stringify(pendente));
            else localStorage.removeItem(chavePendente);
        }
        setSolicitacaoPendente(pendente);
    }

    // Consulta periodicamente a nota sem realizar outro POST de emissão.
    useEffect(() => {
        if (!solicitacaoPendente?.solicitacaoId) return;
        let ativo = true;
        let timer;
        let consultando = false;

        async function verificar() {
            if (!ativo || consultando) return;
            consultando = true;
            setConsultandoAgora(true);
            try {
                const nota = await consultarSolicitacaoNFCe(solicitacaoPendente.solicitacaoId);
                if (!ativo) return;
                const status = String(nota?.status || "").toUpperCase();
                if (status === "AUTORIZADA" && Number(nota?.cStat) === 100) {
                    guardarSolicitacao(null);
                    setEmitindo(false);
                    setItens([]);
                    setPesquisa("");
                    setModalFinalizarAberto(false);
                    setErroEmissao("");
                    setMensagemEmissao(`NFC-e ${nota.numero} autorizada com sucesso. Protocolo: ${nota.protocolo || "-"}`);
                    return;
                }
                if (["REJEITADA", "ERRO", "CANCELADA"].includes(status)) {
                    guardarSolicitacao(null);
                    setEmitindo(false);
                    setErroEmissao(nota?.xMotivo || `NFC-e ${nota?.numero}: ${status}`);
                    return;
                }
                setMensagemEmissao(`NFC-e ${nota?.numero || solicitacaoPendente.numero} aguardando processamento (${status || "PENDENTE"}).`);
            } catch (error) {
                if (ativo) setMensagemEmissao(`Não foi possível consultar a solicitação ${solicitacaoPendente.solicitacaoId}. Não reenvie a venda; a consulta será repetida.`);
            } finally {
                consultando = false;
                if (ativo) {
                    setConsultandoAgora(false);
                    timer = setTimeout(verificar, 5000);
                }
            }
        }
        verificar();
        return () => { ativo = false; clearTimeout(timer); };
    }, [solicitacaoPendente?.solicitacaoId, consultaManual]);

    useEffect(() => {
    async function carregarProdutos() {
        try {
        setCarregando(true);

        const dados = await obterProdutos();

        setProdutos(dados || []);
        } catch (error) {
        console.error("Erro ao carregar produtos:", error);

        setErro(
            error.message ||
            "Não foi possível carregar os produtos."
        );
        } finally {
        setCarregando(false);
        }
    }

    carregarProdutos();
    }, []);

    const produtosFiltrados = useMemo(() => {
    const termo = pesquisa
        .trim()
        .toLowerCase();

    if (!termo) {
        return produtos;
    }

    return produtos.filter((produto) => {
        return (
        String(produto.codigo ?? "")
            .toLowerCase()
            .includes(termo) ||
        String(produto.descricao ?? "")
            .toLowerCase()
            .includes(termo)
        );
    });
    }, [produtos, pesquisa]);

    function formatarMoeda(valor) {
    return Number(valor || 0).toLocaleString(
        "pt-BR",
        {
        style: "currency",
        currency: "BRL",
        }
    );
    }

    function adicionarProduto(produto) {
    setItens((itensAtuais) => {
        const existente = itensAtuais.find(
        (item) => item.id === produto.id
        );

        if (existente) {
        return itensAtuais.map((item) =>
            item.id === produto.id
            ? {
                ...item,
                quantidade: item.quantidade + 1,
                }
            : item
        );
        }

        return [
        ...itensAtuais,
        {
            ...produto,
            quantidade: 1,
        },
        ];
    });
    }

    function alterarQuantidade(produtoId, quantidade) {
    if (quantidade <= 0) {
        setItens((itensAtuais) =>
        itensAtuais.filter(
            (item) => item.id !== produtoId
        )
        );

        return;
    }

    setItens((itensAtuais) =>
        itensAtuais.map((item) =>
        item.id === produtoId
            ? {
                ...item,
                quantidade,
            }
            : item
        )
    );
    }

    function removerProduto(produtoId) {
    setItens((itensAtuais) =>
        itensAtuais.filter(
        (item) => item.id !== produtoId
        )
    );
    }

    const quantidadeTotal = itens.reduce(
    (total, item) =>
        total + item.quantidade,
    0
    );

    const valorTotal = itens.reduce(
    (total, item) =>
        total +
        Number(item.valorVenda) *
        item.quantidade,
    0
    );  

    async function finalizarVenda(dados) {
        if (itens.length === 0 || emitindo || solicitacaoPendente || envioEmCursoRef.current) return;
        if (!empresaId) {
            setErroEmissao("Empresa da sessão não encontrada.");
            return;
        }

        const request = {
            empresaId,
            destinatario: { cpfCnpj: dados.cpf || null },
            produtos: itens.map((item) => ({
                produtoId: item.id,
                quantidade: item.quantidade,
            })),
        };

        envioEmCursoRef.current = true;
        setEmitindo(true);
        setErroEmissao("");
        setMensagemEmissao("Enviando NFC-e para o Fiscal.Agent...");
        try {
            const resultado = await autorizarNFCeAgent(request);
            if (!resultado?.solicitacaoId || !resultado?.pendente) {
                throw new Error("Resposta inesperada da API. Confira a nota antes de tentar novamente.");
            }
            guardarSolicitacao({
                solicitacaoId: resultado.solicitacaoId,
                numero: resultado.numero,
                criadaEm: Date.now(),
                itens: itens.map(item => ({ ...item })),
            });
            setMensagemEmissao(`NFC-e ${resultado.numero} enviada. Aguardando autorização pela SEFAZ...`);
        } catch (error) {
            console.error("Erro ao solicitar NFC-e:", error);
            setErroEmissao(`${error.message || "Falha na comunicação."} Verifique as notas fiscais antes de reenviar: a solicitação pode ter sido registrada.`);
            setMensagemEmissao("");
        } finally {
            envioEmCursoRef.current = false;
            setEmitindo(false);
        }
    }

    return (
    <main className="vendas-pagina">
        <header className="vendas-cabecalho">
        <div>
            <span>VENDAS</span>
            <h2>PDV - Venda</h2>
            <p>
            Selecione os produtos para iniciar uma nova venda.
            </p>
        </div>
        </header>

        {mensagemEmissao && (
            <div className="alert alert-info" role="status">{mensagemEmissao}</div>
        )}
        {solicitacaoPendente && esperaLonga && (
            <div className="alert alert-warning" role="alert">
                <strong>Fiscal.Agent indisponível ou demorando para responder.</strong>{" "}
                NFC-e {solicitacaoPendente.numero} já registrada. Não envie novamente.
                O acompanhamento continuará automaticamente.
                <div style={{ display: "flex", gap: 8, marginTop: 10, flexWrap: "wrap" }}>
                    <button type="button" className="btn btn-outline-primary btn-sm"
                        disabled={consultandoAgora}
                        onClick={() => setConsultaManual(n => n + 1)}>
                        {consultandoAgora ? "Consultando..." : "Consultar situação agora"}
                    </button>
                    <button type="button" className="btn btn-outline-secondary btn-sm"
                        onClick={() => setModalFinalizarAberto(false)}>
                        Fechar janela de finalização
                    </button>
                </div>
            </div>
        )}
        {erroEmissao && !modalFinalizarAberto && (
            <div className="alert alert-danger" role="alert">{erroEmissao}</div>
        )}
        {erro && (
        <div className="vendas-erro">
            {erro}
        </div>
        )}

        <div className="vendas-pdv">

        <section className="vendas-produtos">

            <div className="vendas-painel-titulo">
            <div>
                <span>PRODUTOS</span>
                <h3>Selecione um produto</h3>
            </div>
            </div>

            <div className="vendas-pesquisa">
            <i className="bi bi-search" />

            <input
                type="text"
                value={pesquisa}
                onChange={(e) =>
                setPesquisa(e.target.value)
                }
                placeholder="Código ou descrição do produto..."
            />
            </div>

            {carregando ? (
            <div className="vendas-mensagem">
                Carregando produtos...
            </div>
            ) : (
            <div className="vendas-produtos-grid">

                {produtosFiltrados.map((produto) => (
                    <button
                    type="button"
                    className="vendas-produto-card"
                    key={produto.id}
                    disabled={!!solicitacaoPendente || emitindo}
                     onClick={() => adicionarProduto(produto)}
                    >
                    <div className="vendas-produto-icone">
                    <i className="bi bi-box-seam" />
                    </div>

                    <div className="vendas-produto-info">
                    <small>
                        Cód. {produto.codigo}
                    </small>

                    <strong>
                        {produto.descricao}
                    </strong>

                    <span>
                        {formatarMoeda(
                        produto.valorVenda
                        )}
                    </span>
                    </div>
                </button>
                ))}

                {produtosFiltrados.length === 0 && (
                <div className="vendas-mensagem">
                    Nenhum produto encontrado.
                </div>
                )}

            </div>
            )}

        </section>

        <aside className="vendas-carrinho">

            <div className="vendas-painel-titulo">
            <div>
                <span>VENDA</span>
                <h3>Venda atual</h3>
            </div>

            <div className="vendas-contador">
                <i className="bi bi-cart3" />
                {quantidadeTotal} {quantidadeTotal === 1 ? "item" : "itens"}
            </div>
            </div>

            {itens.length === 0 ? (
            <div className="vendas-carrinho-vazio">
                <i className="bi bi-cart-x" />

                <strong>
                Nenhum produto adicionado
                </strong>

                <span>
                Clique em um produto para adicioná-lo à venda.
                </span>
            </div>
            ) : (
            <div className="vendas-carrinho-itens">
                {itens.map((item) => (
                <div
                    className="vendas-carrinho-item"
                    key={item.id}
                >
                    <div className="vendas-item-info">
                    <small>
                        Cód. {item.codigo}
                    </small>

                    <strong>
                        {item.descricao}
                    </strong>

                    <span>
                        {formatarMoeda(item.valorVenda)}
                    </span>
                    </div>

                    <div className="vendas-item-controles">
                    <button
                        type="button"
                        onClick={() =>
                        alterarQuantidade(
                            item.id,
                            item.quantidade - 1
                        )
                        }
                    >
                        <i className="bi bi-dash" />
                    </button>

                    <strong>
                        {item.quantidade}
                    </strong>

                    <button
                        type="button"
                        onClick={() =>
                        alterarQuantidade(
                            item.id,
                            item.quantidade + 1
                        )
                        }
                    >
                        <i className="bi bi-plus" />
                    </button>

                    <button
                        type="button"
                        className="vendas-item-excluir"
                        onClick={() =>
                        removerProduto(item.id)
                        }
                        title="Remover produto"
                    >
                        <i className="bi bi-trash3" />
                    </button>
                    </div>

                    <div className="vendas-item-subtotal">
                    {formatarMoeda(
                        Number(item.valorVenda) *
                        item.quantidade
                    )}
                    </div>
                </div>
                ))}
            </div>
            )}


            <div className="vendas-total">
            <span>Total da venda</span>
                <strong>
                {formatarMoeda(valorTotal)}
                </strong>
            </div>

            <div className="vendas-acoes">
            <button
                type="button"
                className="vendas-limpar"
                disabled={itens.length === 0 || !!solicitacaoPendente || emitindo}
                onClick={() => {
                setItens([]);
                setPesquisa("");
                setErroEmissao("");
                }}
            >
                <i className="bi bi-trash3" />
                Limpar venda
            </button>

            <button
                type="button"
                className="vendas-finalizar"
                disabled={itens.length === 0 || !!solicitacaoPendente || emitindo}
                onClick={() => setModalFinalizarAberto(true)}
            >
                <i className="bi bi-check-circle" />
                Finalizar venda
            </button>
            </div>

        </aside>

        </div>

    <FinalizarVendaModal
        aberto={modalFinalizarAberto}
        quantidadeItens={quantidadeTotal}
        valorTotal={valorTotal}
        emitindo={emitindo || (!!solicitacaoPendente && !esperaLonga)}
        erroEmissao={erroEmissao}
        onFechar={() => {
            if (!emitindo && (!solicitacaoPendente || esperaLonga)) {
            setErroEmissao("");
            setModalFinalizarAberto(false);
            }
        }}
        onCancelarVenda={() => {
            if (!emitindo && !solicitacaoPendente) {
            setItens([]);
            setPesquisa("");
            setErroEmissao("");
            setModalFinalizarAberto(false);
            }
        }}
    onConfirmar={finalizarVenda}
    />

    </main>
    );
}