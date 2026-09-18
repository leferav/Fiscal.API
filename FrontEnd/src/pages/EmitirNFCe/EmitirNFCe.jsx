import {
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";

import {
  obterProdutosPorEmpresa,
  autorizarNFCe,
} from "../../services/fiscalApi";

import {
  obterEmpresa,
  obterUsuario,
} from "../../services/authService";

import "./EmitirNFCe.css";


const EMPRESA_ID =
  "c5efdb35-f024-476c-9637-80820d00aa88";


export default function EmitirNFCe({ onLogout }) {

  /* ============================================================
     ESTADOS
  ============================================================ */

  const [busca, setBusca] = useState("");

  const [itens, setItens] = useState([]);

  const [
    produtosDisponiveis,
    setProdutosDisponiveis,
  ] = useState([]);

  const [
    carregandoProdutos,
    setCarregandoProdutos,
  ] = useState(true);

  const [
    erroProdutos,
    setErroProdutos,
  ] = useState("");

  const [
    emitindo,
    setEmitindo,
  ] = useState(false);

  const [
    resultadoEmissao,
    setResultadoEmissao,
  ] = useState(null);

  const [
    erroEmissao,
    setErroEmissao,
  ] = useState("");

  const [
    menuUsuarioAberto,
    setMenuUsuarioAberto,
  ] = useState(false);


  /* ============================================================
     REFERÊNCIAS
  ============================================================ */

  const inputBuscaRef = useRef(null);


  /* ============================================================
     SESSÃO
  ============================================================ */

  const usuario = obterUsuario();

  const empresa = obterEmpresa();


  /* ============================================================
     CARREGAR PRODUTOS
  ============================================================ */

  useEffect(() => {

    async function carregarProdutos() {

      try {

        setCarregandoProdutos(true);

        setErroProdutos("");

        const produtos =
          await obterProdutosPorEmpresa(
            EMPRESA_ID
          );

        setProdutosDisponiveis(
          produtos
            .filter(
              (produto) =>
                produto.ativo
            )
            .map(
              (produto) => ({
                id: produto.id,
                codigo: produto.codigo,
                descricao: produto.descricao,
                valorVenda: Number(
                  produto.valorVenda
                ),
              })
            )
        );

      } catch (error) {

        console.error(
          "Erro ao carregar produtos:",
          error
        );

        setErroProdutos(
          "Não foi possível carregar os produtos."
        );

      } finally {

        setCarregandoProdutos(false);

      }

    }

    carregarProdutos();

  }, []);


  /* ============================================================
     FILTRAR PRODUTOS
  ============================================================ */

  const produtosFiltrados =
    useMemo(() => {

      const texto =
        busca
          .trim()
          .toLowerCase();

      if (!texto) {
        return [];
      }

      return produtosDisponiveis.filter(
        (produto) =>
          produto.codigo
            .toLowerCase()
            .includes(texto) ||

          produto.descricao
            .toLowerCase()
            .includes(texto)
      );

    }, [
      busca,
      produtosDisponiveis,
    ]);


  /* ============================================================
     ADICIONAR PRODUTO
  ============================================================ */

  function adicionarProduto(produto) {

    setItens((atual) => {

      const existente =
        atual.find(
          (item) =>
            item.id === produto.id
        );

      if (existente) {

        return atual.map(
          (item) =>
            item.id === produto.id
              ? {
                  ...item,
                  quantidade:
                    item.quantidade + 1,
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


  /* ============================================================
     ENTER NA PESQUISA
  ============================================================ */

  function tratarEnterPesquisa(event) {

    if (event.key !== "Enter") {
      return;
    }

    event.preventDefault();

    const texto =
      busca
        .trim()
        .toLowerCase();

    if (!texto) {
      return;
    }

    const produtoCodigoExato =
      produtosDisponiveis.find(
        (produto) =>
          produto.codigo
            .toLowerCase() === texto
      );

    if (produtoCodigoExato) {

      adicionarProduto(
        produtoCodigoExato
      );

      return;

    }

    if (
      produtosFiltrados.length === 1
    ) {

      adicionarProduto(
        produtosFiltrados[0]
      );

    }

  }


  /* ============================================================
     QUANTIDADE
  ============================================================ */

  function aumentarQuantidade(id) {

    setItens((atual) =>
      atual.map(
        (item) =>
          item.id === id
            ? {
                ...item,
                quantidade:
                  item.quantidade + 1,
              }
            : item
      )
    );

  }


  function diminuirQuantidade(id) {

    setItens((atual) =>
      atual.map(
        (item) =>
          item.id === id
            ? {
                ...item,
                quantidade:
                  Math.max(
                    1,
                    item.quantidade - 1
                  ),
              }
            : item
      )
    );

  }


  function alterarQuantidade(
    id,
    quantidade
  ) {

    const novaQuantidade =
      Number(quantidade);

    if (
      !Number.isFinite(
        novaQuantidade
      ) ||
      novaQuantidade < 1
    ) {
      return;
    }

    setItens((atual) =>
      atual.map(
        (item) =>
          item.id === id
            ? {
                ...item,
                quantidade:
                  novaQuantidade,
              }
            : item
      )
    );

  }


  /* ============================================================
     REMOVER PRODUTO
  ============================================================ */

  function removerProduto(id) {

    setItens((atual) =>
      atual.filter(
        (item) =>
          item.id !== id
      )
    );

    inputBuscaRef.current?.focus();

  }


  /* ============================================================
     LIMPAR VENDA
  ============================================================ */

  function limparVenda() {

    setItens([]);

    setBusca("");

    setResultadoEmissao(null);

    setErroEmissao("");

    inputBuscaRef.current?.focus();

  }


  /* ============================================================
     TOTAIS
  ============================================================ */

  const total =
    useMemo(() => {

      return itens.reduce(
        (soma, item) =>
          soma +
          item.quantidade *
            item.valorVenda,
        0
      );

    }, [itens]);


  const quantidadeTotal =
    useMemo(() => {

      return itens.reduce(
        (soma, item) =>
          soma +
          item.quantidade,
        0
      );

    }, [itens]);


  /* ============================================================
     EMITIR NFC-e
  ============================================================ */

  async function emitirNFCe() {

    if (
      itens.length === 0 ||
      emitindo
    ) {
      return;
    }

    const request = {

      empresaId: EMPRESA_ID,

      produtos: itens.map(
        (item) => ({
          produtoId: item.id,
          quantidade:
            item.quantidade,
        })
      ),

    };

    try {

      setEmitindo(true);

      setErroEmissao("");

      setResultadoEmissao(null);

      console.log(
        "Enviando NFC-e:",
        request
      );

      const resultado =
        await autorizarNFCe(
          request
        );

      console.log(
        "Retorno NFC-e:",
        resultado
      );

      setResultadoEmissao(
        resultado
      );

      alert(
        "NFC-e processada com sucesso."
      );

    } catch (error) {

      console.error(
        "Erro na emissão da NFC-e:",
        error
      );

      setErroEmissao(
        error.message ||
          "Não foi possível emitir a NFC-e."
      );

    } finally {

      setEmitindo(false);

    }

  }


  /* ============================================================
     TELA
  ============================================================ */

  return (

    <div className="pagina">

      {/* ========================================================
          CABEÇALHO
      ======================================================== */}

      <header className="topo">

        <div className="marca">

          <strong>
            FISCAL.API
          </strong>

          <span />

        </div>


        <div className="divisor-topo" />


        <div className="titulo-topo">

          <h1>
            Emissão de NFC-e
          </h1>

          <p>
            Venda para o consumidor final
          </p>

        </div>


        {/* EMPRESA / USUÁRIO */}

        <div className="empresa-topo">

          <div className="header-account">

            <div className="header-company-icon">

              <i className="bi bi-building" />

            </div>


            <div className="header-company">

              <span className="header-company-label">
                Empresa
              </span>

              <strong>

                {empresa?.nomeFantasia ||
                  empresa?.razaoSocial ||
                  "Empresa"}

              </strong>

            </div>


            <div className="header-user-wrapper">

              <button
                type="button"
                className="header-user-button"
                onClick={() =>
                  setMenuUsuarioAberto(
                    (aberto) =>
                      !aberto
                  )
                }
              >

                <div className="header-user-avatar">

                  {usuario?.nome
                    ?.charAt(0)
                    .toUpperCase() ||
                    "U"}

                </div>


                <div className="header-user-info">

                  <strong>

                    {usuario?.nome ||
                      "Usuário"}

                  </strong>

                  <span>

                    {usuario?.perfil ||
                      ""}

                  </span>

                </div>


                <i
                  className={
                    menuUsuarioAberto
                      ? "bi bi-chevron-up header-user-arrow"
                      : "bi bi-chevron-down header-user-arrow"
                  }
                />

              </button>


              {menuUsuarioAberto && (

                <div className="header-user-menu">

                  <div className="header-user-menu-info">

                    <strong>
                      {usuario?.nome}
                    </strong>

                    <span>
                      {usuario?.email}
                    </span>

                  </div>


                  <div className="header-user-menu-divider" />


                  <button
                    type="button"
                    className="header-logout"
                    onClick={onLogout}
                  >

                    <i className="bi bi-box-arrow-right me-2" />

                    Sair do sistema

                  </button>

                </div>

              )}

            </div>

          </div>

        </div>

      </header>


      {/* ========================================================
          CONTEÚDO
      ======================================================== */}

      <main className="conteudo">


        {/* ======================================================
            PESQUISA
        ====================================================== */}

        <section className="card card-produto">

          <div className="cabecalho-produto">

            <div className="icone-carrinho">

              <i className="bi bi-cart3" />

            </div>


            <div>

              <span className="label-azul">
                PRODUTO
              </span>

              <h2>
                Adicionar produto
              </h2>

            </div>

          </div>


          {erroProdutos && (

            <div
              className="alert alert-danger d-flex align-items-center"
              role="alert"
            >

              <i className="bi bi-exclamation-triangle-fill me-2" />

              {erroProdutos}

            </div>

          )}


          <div className="linha-pesquisa">

            <div className="campo-pesquisa">

              <i className="bi bi-search lupa" />


              <input
                ref={inputBuscaRef}
                type="text"
                className="form-control"
                value={busca}
                onChange={(e) =>
                  setBusca(
                    e.target.value
                  )
                }
                onKeyDown={
                  tratarEnterPesquisa
                }
                placeholder={
                  carregandoProdutos
                    ? "Carregando produtos..."
                    : "Digite o código ou descrição do produto..."
                }
                disabled={
                  carregandoProdutos
                }
                autoFocus
              />


              {busca &&
                !carregandoProdutos && (

                  <div className="resultado-pesquisa">

                    {produtosFiltrados.length ===
                    0 ? (

                      <div className="sem-resultado">

                        <i className="bi bi-search me-2" />

                        Nenhum produto encontrado.

                      </div>

                    ) : (

                      produtosFiltrados.map(
                        (produto) => (

                          <button
                            key={
                              produto.id
                            }
                            type="button"
                            onClick={() =>
                              adicionarProduto(
                                produto
                              )
                            }
                          >

                            <div>

                              <strong>
                                {
                                  produto.codigo
                                }
                              </strong>

                              <span>
                                {
                                  produto.descricao
                                }
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

              <i className="bi bi-keyboard me-2" />

              <span>
                Digite o{" "}
                <strong>
                  código
                </strong>{" "}
                e pressione{" "}
                <strong>
                  Enter
                </strong>
              </span>

            </div>

          </div>

        </section>


        {/* ======================================================
            ERRO DE EMISSÃO
        ====================================================== */}

        {erroEmissao && (

          <div
            className="alert alert-danger d-flex align-items-center"
            role="alert"
          >

            <i className="bi bi-exclamation-triangle-fill me-2" />

            {erroEmissao}

          </div>

        )}


        {/* ======================================================
            ITENS
        ====================================================== */}

        <section className="card card-venda">

          <div className="cabecalho-venda">

            <div className="cabecalho-venda-titulo">

              <div className="icone-documento">

                <i className="bi bi-receipt" />

              </div>


              <div>

                <span>
                  VENDA
                </span>

                <h2>
                  Itens da NFC-e
                </h2>

              </div>

            </div>


            <div className="resumo-itens">

              <i className="bi bi-cart3 mini-carrinho" />


              <strong>
                {itens.length}
              </strong>

              <span>

                {itens.length === 1
                  ? "produto"
                  : "produtos"}

              </span>


              <div className="separador" />


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


          {/* TABELA */}

          <div className="table-responsive tabela-container">

            <table className="table align-middle mb-0">

              <thead>

                <tr>

                  <th>
                    Cód.
                  </th>

                  <th>
                    Produto
                  </th>

                  <th>
                    Quantidade
                  </th>

                  <th>
                    Valor unitário
                  </th>

                  <th>
                    Total
                  </th>

                  <th>
                    Ações
                  </th>

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

                        <i className="bi bi-cart-x carrinho-vazio" />

                        <strong>
                          Nenhum produto adicionado.
                        </strong>

                        <span>
                          Pesquise um produto acima para iniciar a venda.
                        </span>

                      </div>

                    </td>

                  </tr>

                ) : (

                  itens.map(
                    (item) => (

                      <tr key={item.id}>

                        {/* CÓDIGO */}

                        <td>

                          <span className="codigo">
                            {item.codigo}
                          </span>

                        </td>


                        {/* PRODUTO */}

                        <td>

                          <strong className="produto-nome">

                            {item.descricao}

                          </strong>

                        </td>


                        {/* QUANTIDADE */}

                        <td>

                          <div className="controle-qtd">

                            <button
                              type="button"
                              className="menos"
                              title="Diminuir quantidade"
                              onClick={() =>
                                diminuirQuantidade(
                                  item.id
                                )
                              }
                            >

                              <i className="bi bi-dash-lg" />

                            </button>


                            <input
                              type="number"
                              min="1"
                              value={
                                item.quantidade
                              }
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
                              title="Aumentar quantidade"
                              onClick={() =>
                                aumentarQuantidade(
                                  item.id
                                )
                              }
                            >

                              <i className="bi bi-plus-lg" />

                            </button>

                          </div>

                        </td>


                        {/* VALOR */}

                        <td>

                          {formatarMoeda(
                            item.valorVenda
                          )}

                        </td>


                        {/* TOTAL */}

                        <td>

                          <strong className="valor-total-item">

                            {formatarMoeda(
                              item.quantidade *
                                item.valorVenda
                            )}

                          </strong>

                        </td>


                        {/* AÇÕES */}

                        <td>

                          <button
                            type="button"
                            className="btn btn-outline-danger btn-remover"
                            onClick={() =>
                              removerProduto(
                                item.id
                              )
                            }
                          >

                            <i className="bi bi-trash3 me-2" />

                            Remover

                          </button>

                        </td>

                      </tr>

                    )
                  )

                )}

              </tbody>

            </table>

          </div>

        </section>


        {/* ======================================================
            RODAPÉ DA VENDA
        ====================================================== */}

        <section className="card rodape-venda">


          {/* LIMPAR */}

          <button
            type="button"
            className="btn btn-outline-danger btn-limpar"
            onClick={
              limparVenda
            }
            disabled={
              itens.length === 0
            }
          >

            <i className="bi bi-trash3" />

            Limpar venda

          </button>


          <div className="separador-rodape" />


          {/* UNIDADES */}

          <div className="total-unidades">

            <div className="icone-unidades">

              <i className="bi bi-box-seam" />

            </div>


            <div>

              <span>
                Total de unidades
              </span>

              <strong>

                {quantidadeTotal}{" "}

                {quantidadeTotal === 1
                  ? "unidade"
                  : "unidades"}

              </strong>

            </div>

          </div>


          <div className="espacador" />


          {/* TOTAL */}

          <div className="total-nfce">

            <span>
              Total da NFC-e
            </span>

            <strong>

              {formatarMoeda(
                total
              )}

            </strong>

          </div>


          {/* EMITIR */}

          <button
            type="button"
            className="btn btn-emitir"
            onClick={
              emitirNFCe
            }
            disabled={
              itens.length === 0 ||
              emitindo
            }
          >

            {emitindo ? (

              <>

                <span
                  className="spinner-border spinner-border-sm"
                  aria-hidden="true"
                />

                Emitindo...

              </>

            ) : (

              <>

                <i className="bi bi-receipt" />

                Emitir NFC-e

              </>

            )}

          </button>

        </section>


        {/* ======================================================
            RETORNO DA EMISSÃO
        ====================================================== */}

        {resultadoEmissao && (

          <div className="alert alert-success mt-3">

            <div className="d-flex align-items-center">

              <i className="bi bi-check-circle-fill me-2" />

              <strong>
                NFC-e processada com sucesso.
              </strong>

            </div>

          </div>

        )}

      </main>

    </div>

  );

}


/* ============================================================
   FORMATAR MOEDA
============================================================ */

function formatarMoeda(valor) {

  return Number(
    valor || 0
  ).toLocaleString(
    "pt-BR",
    {
      style: "currency",
      currency: "BRL",
    }
  );

}