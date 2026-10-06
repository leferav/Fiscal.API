import { useEffect, useMemo, useState } from "react";
import { obterProdutos } from "../../services/produtoService";
import FinalizarVendaModal from "./FinalizarVendaModal";
import { autorizarNFCe } from "../../services/fiscalApi";
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
    if (itens.length === 0 || emitindo) {
        return;
    }

    if (!empresaId) {
        setErroEmissao("Empresa da sessão não encontrada.");
        return;
    }

    const request = {
        empresaId,

        destinatario: {
        cpfCnpj: dados.cpf || null,
        },

        produtos: itens.map((item) => ({
        produtoId: item.id,
        quantidade: item.quantidade,
        })),
    };

    try {
        setEmitindo(true);
        setErroEmissao("");

        const resultado = await autorizarNFCe(request);

        console.log("Retorno NFC-e:", resultado);

        if (Number(resultado?.cStat) !== 100) {
        setErroEmissao(
            resultado?.xMotivo ||
            "A NFC-e não foi autorizada pela SEFAZ."
        );

        return;
        }

        // Somente limpa a venda se realmente foi autorizada.
        setItens([]);
        setPesquisa("");
        setModalFinalizarAberto(false);

        alert(
        `NFC-e ${resultado.numero ?? ""} autorizada com sucesso.`
        );
    } catch (error) {
        console.error("Erro na emissão da NFC-e:", error);

        setErroEmissao(
        error.message ||
            "Não foi possível emitir a NFC-e."
        );
    } finally {
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
                disabled={itens.length === 0}
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
                disabled={itens.length === 0}
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
        emitindo={emitindo}
        erroEmissao={erroEmissao}
        onFechar={() => {
            if (!emitindo) {
            setErroEmissao("");
            setModalFinalizarAberto(false);
            }
        }}
        onCancelarVenda={() => {
            if (!emitindo) {
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