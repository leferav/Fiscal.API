import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { obterProdutoPorId } from "../../services/produtoService";
import "./ProdutoDetalhes.css";

export default function ProdutoDetalhes() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [produto, setProduto] = useState(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  useEffect(() => {
    async function carregarProduto() {
      try {
        setCarregando(true);
        setErro("");

        const dados = await obterProdutoPorId(id);
        setProduto(dados);
      } catch (error) {
        console.error("Erro ao carregar produto:", error);

        setErro(
          error.message ||
            "Não foi possível carregar o produto."
        );
      } finally {
        setCarregando(false);
      }
    }

    carregarProduto();
  }, [id]);

  if (carregando) {
    return (
      <main className="conteudo">
        <div className="card produto-detalhe-card">
          Carregando produto...
        </div>
      </main>
    );
  }

  if (erro) {
    return (
      <main className="conteudo">
        <div className="card produto-detalhe-card">
          <div className="alert alert-danger">
            {erro}
          </div>

          <button
            type="button"
            className="btn btn-outline-primary"
            onClick={() => navigate("/cadastros/produtos")}
          >
            Voltar
          </button>
        </div>
      </main>
    );
  }

  return (
    <main className="conteudo produto-detalhe-pagina">

      <button
        type="button"
        className="produto-detalhe-voltar"
        onClick={() => navigate("/cadastros/produtos")}
      >
        <i className="bi bi-arrow-left" />
        Voltar para Produtos
      </button>

      <section className="produto-detalhe-cabecalho">
        <div>
          <span className="label-azul">
            CADASTROS
          </span>

          <div className="produto-detalhe-titulo">
            <h2>{produto.descricao}</h2>

            <span
              className={`produto-status ${
                produto.ativo ? "ativo" : "inativo"
              }`}
            >
              {produto.ativo ? "ATIVO" : "INATIVO"}
            </span>
          </div>

          <p>Produto {produto.codigo}</p>
        </div>
      </section>

      <section className="card produto-detalhe-card">

        <div className="produto-card-titulo">
          <div className="produto-card-icone">
            <i className="bi bi-box-seam" />
          </div>

          <div>
            <span className="label-azul">
              PRODUTO
            </span>
            <h3>Dados do produto</h3>
          </div>
        </div>

        <div className="produto-detalhe-grid">
          <Info
            titulo="Código"
            valor={produto.codigo}
          />

          <Info
            titulo="Descrição"
            valor={produto.descricao}
          />

          <Info
            titulo="NCM"
            valor={produto.ncm}
          />

          <Info
            titulo="Unidade"
            valor={produto.unidade}
          />

          <Info
            titulo="Valor de venda"
            valor={formatarMoeda(produto.valorVenda)}
          />
        </div>

      </section>

      <section className="card produto-detalhe-card">

        <div className="produto-card-titulo">
          <div className="produto-card-icone">
            <i className="bi bi-receipt" />
          </div>

          <div>
            <span className="label-azul">
              TRIBUTAÇÃO
            </span>
            <h3>Configuração tributária</h3>
          </div>
        </div>

        {produto.tributacao ? (
          <div className="produto-detalhe-grid">

            <Info
              titulo="Configuração"
              valor={produto.tributacao.nome}
            />

            <Info
              titulo="CFOP"
              valor={produto.tributacao.cfop}
            />

            <Info
              titulo="CST ICMS"
              valor={produto.tributacao.cstIcms || "-"}
            />

            <Info
              titulo="CSOSN"
              valor={produto.tributacao.csosn || "-"}
            />

            <Info
              titulo="Alíquota ICMS"
              valor={`${produto.tributacao.aliquotaIcms}%`}
            />

          </div>
        ) : (
          <p>Nenhuma configuração tributária vinculada.</p>
        )}

      </section>

      <section className="produto-detalhe-acoes">

        <button
          type="button"
          className="btn btn-outline-secondary"
          onClick={() => navigate("/cadastros/produtos")}
        >
          <i className="bi bi-arrow-left" />
          Voltar
        </button>

        <button
        type="button"
        className="btn btn-primary"
        onClick={() =>
            navigate(`/cadastros/produtos/${produto.id}/editar`)
        }
        >
        <i className="bi bi-pencil" />
        Editar produto
        </button>

      </section>

    </main>
  );
}

function Info({ titulo, valor }) {
  return (
    <div className="produto-info">
      <span>{titulo}</span>
      <strong>{valor ?? "-"}</strong>
    </div>
  );
}

function formatarMoeda(valor) {
  return Number(valor || 0).toLocaleString("pt-BR", {
    style: "currency",
    currency: "BRL",
  });
}