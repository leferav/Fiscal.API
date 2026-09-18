import { useEffect, useMemo, useRef, useState } from "react";
import {
          obterProdutosPorEmpresa,
          autorizarNFCe,
        } from "../services/fiscalApi";

const EMPRESA_ID = "c5efdb35-f024-476c-9637-80820d00aa88";

export default function EmitirNFCe() {
  const [busca, setBusca] = useState("");
  const [itens, setItens] = useState([]);
  const [produtosDisponiveis, setProdutosDisponiveis] = useState([]);
  const [carregandoProdutos, setCarregandoProdutos] = useState(true);
  const [erroProdutos, setErroProdutos] = useState("");
  const [emitindo, setEmitindo] = useState(false);
  const [resultadoEmissao, setResultadoEmissao] = useState(null);
  const [erroEmissao, setErroEmissao] = useState("");

  const inputBuscaRef = useRef(null);

  useEffect(() => {
    async function carregarProdutos() {
      try {
        setCarregandoProdutos(true);
        setErroProdutos("");

        const produtos = await obterProdutosPorEmpresa(EMPRESA_ID);

        setProdutosDisponiveis(
          produtos
            .filter((produto) => produto.ativo)
            .map((produto) => ({
              id: produto.id,
              codigo: produto.codigo,
              descricao: produto.descricao,
              valorVenda: Number(produto.valorVenda),
            }))
        );
      } catch (error) {
        console.error("Erro ao carregar produtos:", error);
        setErroProdutos("Não foi possível carregar os produtos.");
      } finally {
        setCarregandoProdutos(false);
      }
    }

    carregarProdutos();
  }, []);

  const produtosFiltrados = useMemo(() => {
    const texto = busca.trim().toLowerCase();

    if (!texto) return [];

    return produtosDisponiveis.filter(
      (produto) =>
        produto.codigo.toLowerCase().includes(texto) ||
        produto.descricao.toLowerCase().includes(texto)
    );
  }, [busca, produtosDisponiveis]);

  function adicionarProduto(produto) {
    setItens((atual) => {
      const existente = atual.find(
        (item) => item.id === produto.id
      );

      if (existente) {
        return atual.map((item) =>
          item.id === produto.id
            ? {
                ...item,
                quantidade: item.quantidade + 1,
              }
            : item
        );
      }

      return [
        ...atual,
        {
          ...produto,
          quantidade: 1,
        },
      ];
    });

    setBusca("");

    setTimeout(() => {
      inputBuscaRef.current?.focus();
    }, 0);
  }

  function tratarEnterPesquisa(event) {
    if (event.key !== "Enter") return;

    event.preventDefault();

    const texto = busca.trim().toLowerCase();

    if (!texto) return;

    const produtoCodigoExato = produtosDisponiveis.find(
      (produto) =>
        produto.codigo.toLowerCase() === texto
    );

    if (produtoCodigoExato) {
      adicionarProduto(produtoCodigoExato);
      return;
    }

    if (produtosFiltrados.length === 1) {
      adicionarProduto(produtosFiltrados[0]);
    }
  }

  function aumentarQuantidade(id) {
    setItens((atual) =>
      atual.map((item) =>
        item.id === id
          ? {
              ...item,
              quantidade: item.quantidade + 1,
            }
          : item
      )
    );
  }

  function diminuirQuantidade(id) {
    setItens((atual) =>
      atual.map((item) =>
        item.id === id
          ? {
              ...item,
              quantidade: Math.max(
                1,
                item.quantidade - 1
              ),
            }
          : item
      )
    );
  }

  function alterarQuantidade(id, quantidade) {
    const novaQuantidade = Number(quantidade);

    if (
      !Number.isFinite(novaQuantidade) ||
      novaQuantidade < 1
    ) {
      return;
    }

    setItens((atual) =>
      atual.map((item) =>
        item.id === id
          ? {
              ...item,
              quantidade: novaQuantidade,
            }
          : item
      )
    );
  }

  function removerProduto(id) {
    setItens((atual) =>
      atual.filter((item) => item.id !== id)
    );

    inputBuscaRef.current?.focus();
  }

  function limparVenda() {
    setItens([]);
    setBusca("");
    inputBuscaRef.current?.focus();
  }

  const total = useMemo(() => {
    return itens.reduce(
      (soma, item) =>
        soma + item.quantidade * item.valorVenda,
      0
    );
  }, [itens]);

  const quantidadeTotal = useMemo(() => {
    return itens.reduce(
      (soma, item) => soma + item.quantidade,
      0
    );
  }, [itens]);

  async function emitirNFCe() {
    if (itens.length === 0 || emitindo) {
      return;
    }

    const request = {
      empresaId: EMPRESA_ID,

      produtos: itens.map((item) => ({
        produtoId: item.id,
        quantidade: item.quantidade,
      })),
    };

    try {
      setEmitindo(true);
      setErroEmissao("");
      setResultadoEmissao(null);

      console.log("Enviando NFC-e:", request);

      const resultado = await autorizarNFCe(request);

      console.log("Retorno NFC-e:", resultado);

      setResultadoEmissao(resultado);

      alert("NFC-e processada com sucesso.");

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
    <div className="pagina">

      <header className="topo">
        <div className="marca">
          <strong>FISCAL.API</strong>
          <span></span>
        </div>

        <div className="divisor-topo"></div>

        <div className="titulo-topo">
          <h1>Emissão de NFC-e</h1>
          <p>Venda para o consumidor final</p>
        </div>

        <div className="empresa-topo">
          <div className="empresa-icone">
            ▣
          </div>

          <div>
            <span>Empresa</span>
            <strong>Feedback Informática</strong>
          </div>
        </div>
      </header>

      <main className="conteudo">

        {/* BUSCA */}

        <section className="card card-produto">
          <div className="cabecalho-produto">
            <div className="icone-carrinho">
              🛒
            </div>

            <div>
              <span className="label-azul">
                PRODUTO
              </span>

              <h2>Adicionar produto</h2>
            </div>
          </div>

          {erroProdutos && (
            <div className="mensagem-erro">
              {erroProdutos}
            </div>
          )}

          <div className="linha-pesquisa">
            <div className="campo-pesquisa">
              <span className="lupa">
                ⌕
              </span>

              <input
                ref={inputBuscaRef}
                value={busca}
                onChange={(e) =>
                  setBusca(e.target.value)
                }
                onKeyDown={tratarEnterPesquisa}
                placeholder={
                  carregandoProdutos
                    ? "Carregando produtos..."
                    : "Digite o código ou descrição do produto..."
                }
                disabled={carregandoProdutos}
                autoFocus
              />

              {busca && !carregandoProdutos && (
                <div className="resultado-pesquisa">
                  {produtosFiltrados.length === 0 ? (
                    <div className="sem-resultado">
                      Nenhum produto encontrado.
                    </div>
                  ) : (
                    produtosFiltrados.map(
                      (produto) => (
                        <button
                          key={produto.id}
                          type="button"
                          onClick={() =>
                            adicionarProduto(
                              produto
                            )
                          }
                        >
                          <div>
                            <strong>
                              {produto.codigo}
                            </strong>

                            <span>
                              {produto.descricao}
                            </span>
                          </div>

                          <strong>
                            {formatarMoeda(
                              produto.valorVenda
                            )}
                          </strong>
                        </button>
                      )
                    )
                  )}
                </div>
              )}
            </div>

            <div className="dica-enter">
              Digite o <strong>código</strong> e
              <br />
              pressione <strong>Enter</strong>
            </div>
          </div>
        </section>

        {/* ITENS */}

        <section className="card card-venda">

          <div className="cabecalho-venda">
            <div className="cabecalho-venda-titulo">
              <div className="icone-documento">
                ▤
              </div>

              <div>
                <span>VENDA</span>
                <h2>Itens da NFC-e</h2>
              </div>
            </div>

            <div className="resumo-itens">
              <span className="mini-carrinho">
                🛒
              </span>

              <strong>
                {itens.length}
              </strong>

              <span>
                {itens.length === 1
                  ? "produto"
                  : "produtos"}
              </span>

              <div className="separador"></div>

              <strong>
                {quantidadeTotal}
              </strong>

              <span>
                {quantidadeTotal === 1
                  ? "unidade"
                  : "unidades"}
              </span>
            </div>
          </div>

          <div className="tabela-container">
            <table>
              <thead>
                <tr>
                  <th>Cód.</th>
                  <th>Produto</th>
                  <th>Quantidade</th>
                  <th>Valor unitário</th>
                  <th>Total</th>
                  <th>Ações</th>
                </tr>
              </thead>

              <tbody>
                {itens.length === 0 ? (
                  <tr>
                    <td
                      colSpan="6"
                      className="vazio"
                    >
                      <div className="vazio-conteudo">
                        <div className="carrinho-vazio">
                          🛒
                        </div>

                        <strong>
                          Nenhum produto adicionado.
                        </strong>

                        <span>
                          Pesquise um produto acima
                          para iniciar a venda.
                        </span>
                      </div>
                    </td>
                  </tr>
                ) : (
                  itens.map((item) => (
                    <tr key={item.id}>
                      <td className="codigo">
                        {item.codigo}
                      </td>

                      <td>
                        <strong className="produto-nome">
                          {item.descricao}
                        </strong>
                      </td>

                      <td>
                        <div className="controle-qtd">

                          <button
                            type="button"
                            className="menos"
                            onClick={() =>
                              diminuirQuantidade(
                                item.id
                              )
                            }
                          >
                            −
                          </button>

                          <input
                            type="number"
                            min="1"
                            value={item.quantidade}
                            onChange={(e) =>
                              alterarQuantidade(
                                item.id,
                                e.target.value
                              )
                            }
                          />

                          <button
                            type="button"
                            className="mais"
                            onClick={() =>
                              aumentarQuantidade(
                                item.id
                              )
                            }
                          >
                            +
                          </button>

                        </div>
                      </td>

                      <td>
                        {formatarMoeda(
                          item.valorVenda
                        )}
                      </td>

                      <td>
                        <strong className="valor-total-item">
                          {formatarMoeda(
                            item.quantidade *
                              item.valorVenda
                          )}
                        </strong>
                      </td>

                      <td>
                        <button
                          type="button"
                          className="btn-remover"
                          onClick={() =>
                            removerProduto(item.id)
                          }
                        >
                          <span>▣</span>
                          Remover
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </section>

        {/* RODAPÉ */}

        <section className="card rodape-venda">

          <button
            type="button"
            className="btn-limpar"
            onClick={limparVenda}
            disabled={itens.length === 0}
          >
            <span>▣</span>
            Limpar venda
          </button>

          <div className="separador-rodape"></div>

          <div className="total-unidades">
            <div className="icone-unidades">
              ◇
            </div>

            <div>
              <span>Total de unidades</span>

              <strong>
                {quantidadeTotal}{" "}
                {quantidadeTotal === 1
                  ? "unidade"
                  : "unidades"}
              </strong>
            </div>
          </div>

          <div className="espacador"></div>

          <div className="total-nfce">
            <span>Total da NFC-e</span>

            <strong>
              {formatarMoeda(total)}
            </strong>
          </div>

          <button
            type="button"
            className="btn-emitir"
            onClick={emitirNFCe}
            disabled={itens.length === 0 || emitindo}
          >
            <span>▤</span>

            {emitindo
              ? "Emitindo..."
              : "Emitir NFC-e"}
          </button>

        </section>

      </main>
    </div>
  );
}

function formatarMoeda(valor) {
  return Number(valor || 0).toLocaleString(
    "pt-BR",
    {
      style: "currency",
      currency: "BRL",
    }
  );
}