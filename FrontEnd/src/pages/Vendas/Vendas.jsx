import { useEffect, useMemo, useState } from "react";
import { obterProdutos, obterImagemProduto } from "../../services/produtoService";
import FinalizarVendaModal from "./FinalizarVendaModal";
import { autorizarNFCe, salvarVendaPendente } from "../../services/fiscalApi";
import { obterClientes } from "../../services/clienteService";
import { obterEmpresaId } from "../../services/authService";
import "./Vendas.css";

export default function Vendas() {
    const [produtos, setProdutos] = useState([]);
    const [imagensProdutos, setImagensProdutos] = useState({});    
    const [pesquisa, setPesquisa] = useState("");
    const [carregando, setCarregando] = useState(true);
    const [erro, setErro] = useState("");
    const [itens, setItens] = useState([]);
    const [modalFinalizarAberto, setModalFinalizarAberto] = useState(false);
    const [emitindo, setEmitindo] = useState(false);
    const [erroEmissao, setErroEmissao] = useState("");
    const [clientes, setClientes] = useState([]);
    const [pesquisaCliente, setPesquisaCliente] = useState("");
    const [clienteSelecionado, setClienteSelecionado] = useState(null);
    const [erroClientes, setErroClientes] = useState("");
    const empresaId = obterEmpresaId();

    useEffect(() => {
        let ativo = true;
        obterClientes().then((dados) => {
            if (ativo) setClientes(Array.isArray(dados) ? dados : (dados?.clientes ?? dados?.itens ?? []));
        }).catch((error) => {
            if (ativo) setErroClientes(error.message || "Não foi possível carregar os clientes.");
        });
        return () => { ativo = false; };
    }, []);

    const clientesFiltrados = useMemo(() => {
        const termo = pesquisaCliente.trim().toLowerCase();
        if (!termo) return [];
        return clientes.filter((c) => c.ativo !== false &&
            [c.nomeRazaoSocial, c.cpfCnpj].some((v) => String(v ?? "").toLowerCase().includes(termo))).slice(0, 8);
    }, [clientes, pesquisaCliente]);


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

    
    useEffect(() => {
        let cancelado = false;
        const urlsCriadas = [];

        async function carregarImagens() {
            // Carrega as imagens em pequenos grupos para
            // evitar muitas requisições simultâneas.
            const tamanhoLote = 5;

            for (let i = 0; i < produtos.length; i += tamanhoLote) {
                if (cancelado) break;

                const lote = produtos.slice(i, i + tamanhoLote);

                await Promise.all(
                    lote.map(async (produto) => {
                        try {
                            const url = await obterImagemProduto(produto.id);

                            if (!url) return;

                            urlsCriadas.push(url);

                            if (!cancelado) {
                                setImagensProdutos((atual) => ({
                                    ...atual,
                                    [produto.id]: url
                                }));
                            }
                        } catch (error) {
                            console.error(
                                `Erro ao carregar imagem do produto ${produto.id}:`,
                                error
                            );
                        }
                    })
                );
            }
        }

        setImagensProdutos({});
        carregarImagens();

        return () => {
            cancelado = true;
            urlsCriadas.forEach((url) => URL.revokeObjectURL(url));
        };
    }, [produtos]);


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

    async function enviarDepois(dados) {
        if (itens.length === 0 || emitindo) return;

        if (!empresaId) {
            setErroEmissao("Empresa da sessão não encontrada.");
            return;
        }

        const request = {
            formaPagamento: dados.formaPagamento,
            clienteId: clienteSelecionado?.id ?? null,
            cpfConsumidor: clienteSelecionado ? null : (dados.cpf || null),
            produtos: itens.map((item) => ({
                produtoId: item.id,
                quantidade: item.quantidade,
            })),
        };

        try {
            setEmitindo(true);
            setErroEmissao("");

            const resultado = await salvarVendaPendente(request);
            if (resultado?.status !== "A_ENVIAR" || !resultado?.id) {
                throw new Error("Não foi possível confirmar o salvamento da venda.");
            }

            setItens([]);
            setClienteSelecionado(null);
            setPesquisaCliente("");
            setPesquisa("");
            setModalFinalizarAberto(false);
            alert("Venda salva com sucesso para emissão posterior.");
        } catch (error) {
            console.error("Erro ao salvar venda pendente:", error);
            setErroEmissao(error.message || "Não foi possível salvar a venda.");
        } finally {
            setEmitindo(false);
        }
    }

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
        clienteId: clienteSelecionado?.id ?? null,

        destinatario: {
        cpfCnpj: clienteSelecionado ? null : (dados.cpf || null),
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
            const motivo = resultado?.mensagem || resultado?.erro || resultado?.message ||
                resultado?.xMotivo || resultado?.motivo || resultado?.Motivo;
            const codigo = resultado?.cStat != null ? ` (cStat ${resultado.cStat})` : "";
            setErroEmissao(motivo
                ? `${motivo}${codigo}`
                : `A NFC-e não foi autorizada pela SEFAZ${codigo}. Consulte o registro fiscal antes de tentar novamente.`);
            return;
        }

        // Somente limpa a venda se realmente foi autorizada.
        setItens([]);
        setClienteSelecionado(null);
        setPesquisaCliente("");
        setPesquisa("");
        setModalFinalizarAberto(false);

        alert(
        `NFC-e ${resultado.numero ?? ""} autorizada com sucesso.`
        );
    } catch (error) {
        console.error("Erro na emissão da NFC-e:", error);

        const mensagem = error?.response?.data?.mensagem || error?.response?.data?.erro ||
            error?.message || "Não foi possível emitir a NFC-e.";
        const falhaComunicacao = /não foi possível conectar|falha de comunicação|network|failed to fetch|timeout/i.test(mensagem);
        setErroEmissao(falhaComunicacao
            ? `${mensagem} Consulte a situação da NFC-e antes de tentar novamente, para evitar duplicidade.`
            : mensagem);
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
                        {imagensProdutos[produto.id] ? (
                            <img
                                src={imagensProdutos[produto.id]}
                                alt={produto.descricao}
                                className="vendas-produto-imagem"
                            />
                        ) : (
                            <i className="bi bi-box-seam" />
                        )}
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
                setClienteSelecionado(null);
                setPesquisaCliente("");
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
        clienteSelecionado={clienteSelecionado}
        pesquisaCliente={pesquisaCliente}
        setPesquisaCliente={setPesquisaCliente}
        setClienteSelecionado={setClienteSelecionado}
        clientesFiltrados={clientesFiltrados}
        erroClientes={erroClientes}
        onFechar={() => {
            if (!emitindo) {
            setErroEmissao("");
            setModalFinalizarAberto(false);
            }
        }}
        onCancelarVenda={() => {
            if (!emitindo) {
            setItens([]);
            setClienteSelecionado(null);
            setPesquisaCliente("");
            setPesquisa("");
            setErroEmissao("");
            setModalFinalizarAberto(false);
            }
        }}
    onEnviarDepois={enviarDepois}
    onConfirmar={finalizarVenda}
    />

    </main>
    );
}