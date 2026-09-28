import { useEffect, useMemo, useState } from "react";
import { obterProdutos } from "../../services/produtoService";
import { useNavigate } from "react-router-dom";
import "./Produtos.css";

export default function Produtos() {
  const [produtos, setProdutos] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");
  const [pesquisa, setPesquisa] = useState("");
  const navigate = useNavigate();

  useEffect(() => {
    async function carregarProdutos() {
      try {
        setCarregando(true);
        setErro("");

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
    const termo = pesquisa.trim().toLowerCase();

    if (!termo) {
      return produtos;
    }

    return produtos.filter((produto) => {
      return (
        String(produto.codigo || "")
          .toLowerCase()
          .includes(termo) ||
        String(produto.descricao || "")
          .toLowerCase()
          .includes(termo) ||
        String(produto.ncm || "")
          .toLowerCase()
          .includes(termo)
      );
    });
  }, [produtos, pesquisa]);

  return (
    <main className="conteudo produtos-pagina">
      <section className="produtos-cabecalho">
        <div>
          <span className="label-azul">CADASTROS</span>
          <h2>Produtos</h2>
          <p>
            Gerencie os produtos utilizados na emissão
            de documentos fiscais.
          </p>
        </div>

        <button
            type="button"
            className="btn-novo-produto"
            onClick={() => navigate("/cadastros/produtos/novo")}
            >
            <i className="bi bi-plus-lg" />
            Novo produto
        </button>
      </section>

      <section className="card produtos-card">
        <div className="produtos-filtros">
          <div className="produtos-pesquisa">
            <i className="bi bi-search" />

            <input
              type="text"
              value={pesquisa}
              onChange={(e) => setPesquisa(e.target.value)}
              placeholder="Pesquisar por código, descrição ou NCM..."
            />
          </div>
        </div>

        {erro && (
          <div className="alert alert-danger">
            {erro}
          </div>
        )}

        {carregando ? (
          <div className="produtos-vazio">
            Carregando produtos...
          </div>
        ) : (
          <div className="table-responsive">
            <table className="produtos-tabela">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Descrição</th>
                  <th>NCM</th>
                  <th>Unidade</th>
                  <th>Valor</th>
                  <th>Ações</th>
                </tr>
              </thead>

              <tbody>
                {produtosFiltrados.map((produto) => (
                  <tr key={produto.id}>
                    <td>
                      <strong>{produto.codigo}</strong>
                    </td>

                    <td>{produto.descricao}</td>

                    <td>{produto.ncm}</td>

                    <td>{produto.unidade}</td>

                    <td>
                      {formatarMoeda(produto.valorVenda)}
                    </td>

                    <td>
                      <button
                        type="button"
                        className="produto-btn-acao"
                        title="Visualizar produto"
                      >
                        <i className="bi bi-eye" />
                      </button>
                    </td>
                  </tr>
                ))}

                {produtosFiltrados.length === 0 && (
                  <tr>
                    <td
                      colSpan="6"
                      className="produtos-vazio"
                    >
                      Nenhum produto encontrado.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  );
}

function formatarMoeda(valor) {
  return Number(valor || 0).toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL",
  });
}