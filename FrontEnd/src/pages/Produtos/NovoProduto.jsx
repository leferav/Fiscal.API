import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { cadastrarProduto, alterarProduto, obterConfiguracoesTributarias, obterProdutoPorId, pesquisarNcms,} from "../../services/produtoService";
import "./Produtos.css";

export default function NovoProduto() {
  const navigate = useNavigate();
  const { id } = useParams();
  const editando = Boolean(id);
  const [configuracoes, setConfiguracoes] = useState([]);
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState("");
  const [buscaNcm, setBuscaNcm] = useState("");
  const [resultadosNcm, setResultadosNcm] = useState([]);
  const [pesquisandoNcm, setPesquisandoNcm] = useState(false);
  const [form, setForm] = useState({ codigo: "", descricao: "", ncm: "", unidade: "UN", valorVenda: "", configuracaoTributariaId: "", });

  useEffect(() => {
  async function carregarConfiguracoes() {
      try {
      const dados = await obterConfiguracoesTributarias();

      console.log("CONFIGURAÇÕES:", dados);

      setConfiguracoes(dados || []);
      } catch (error) {
      console.error(
          "Erro ao carregar configurações tributárias:",
          error
      );

      setErro(
          error.message ||
          "Não foi possível carregar as configurações tributárias."
      );
      }
  }

  carregarConfiguracoes();
  }, []);

  useEffect(() => {
  if (!editando) {
      return;
  }

  async function carregarProduto() {
      try {
      setErro("");

      const produto = await obterProdutoPorId(id);

      console.log("PRODUTO EDIÇÃO:", produto);

      setForm({
          codigo: produto.codigo || "",
          descricao: produto.descricao || "",
          ncm: produto.ncm || "",
          unidade: produto.unidade || "UN",
          valorVenda: produto.valorVenda ?? "",
          configuracaoTributariaId:
          produto.configuracaoTributariaId || "",
      });
      } catch (error) {
      console.error(
          "Erro ao carregar produto para edição:",
          error
      );

      setErro(
          error.message ||
          "Não foi possível carregar o produto."
      );
      }
  }

  carregarProduto();
  }, [id, editando]);

  function alterarCampo(event) {
  const { name, value } = event.target;

  setForm((anterior) => ({
      ...anterior,
      [name]: value,
  }));
  }

  function alterarCampo(event) {
    const { name, value } = event.target;

    setForm((anterior) => ({
      ...anterior,
      [name]: value,
    }));
  }

  async function alterarBuscaNcm(event) {
  const valor = event.target.value;

  setBuscaNcm(valor);

  // Se o usuário alterar a pesquisa,
  // remove o NCM anteriormente selecionado.
  setForm((anterior) => ({
    ...anterior,
    ncm: "",
  }));

  if (valor.trim().length < 2) {
    setResultadosNcm([]);
    return;
  }

  try {
    setPesquisandoNcm(true);

    const dados = await pesquisarNcms(valor.trim());

    setResultadosNcm(dados || []);
  } catch (error) {
    console.error("Erro ao pesquisar NCM:", error);
    setResultadosNcm([]);
  } finally {
    setPesquisandoNcm(false);
  }
  }

  function selecionarNcm(ncm) {
    setForm((anterior) => ({
      ...anterior,
      ncm: ncm.codigo,
    }));

    setBuscaNcm(
      `${ncm.codigo} - ${ncm.descricao}`
    );

    setResultadosNcm([]);
  }

  async function handleSubmit(event) {
    event.preventDefault();

    try {
      setSalvando(true);
      setErro("");

      if (!form.codigo.trim()) {
        throw new Error("Informe o código do produto.");
      }

      if (!form.descricao.trim()) {
        throw new Error("Informe a descrição do produto.");
      }

      if (!form.ncm.trim()) {
        throw new Error("Informe o NCM.");
      }

      if (!form.configuracaoTributariaId) {
        throw new Error("Selecione a configuração tributária.");
      }

      const valorVenda = Number(
        String(form.valorVenda).replace(",", ".")
      );

      if (!valorVenda || valorVenda <= 0) {
        throw new Error(
          "Informe um valor de venda maior que zero."
        );
      }

    const dadosProduto = {
    codigo: form.codigo.trim(),
    descricao: form.descricao.trim(),
    ncm: form.ncm.trim(),
    unidade: form.unidade,
    valorVenda,
    configuracaoTributariaId:
        form.configuracaoTributariaId,
    };

    if (editando) {
    await alterarProduto(id, dadosProduto);
    } else {
    await cadastrarProduto(dadosProduto);
    }

    navigate(
    editando
        ? `/cadastros/produtos/${id}`
        : "/cadastros/produtos"
    );

      navigate("/cadastros/produtos");
    } catch (error) {
      console.error("Erro ao cadastrar produto:", error);

      setErro(
        error.message ||
          "Não foi possível cadastrar o produto."
      );
    } finally {
      setSalvando(false);
    }
  }

  return (
    <main className="conteudo produtos-pagina">
      <button
        type="button"
        className="detalhe-voltar"
        onClick={() => navigate("/cadastros/produtos")}
      >
        <i className="bi bi-arrow-left" />
        Voltar para Produtos
      </button>

      <section className="produtos-cabecalho">
        <div>
          <span className="label-azul">CADASTROS</span>
            <h2>
            {editando ? "Editar produto" : "Novo produto"}
            </h2>

            <p>
            {editando
                ? "Altere as informações comerciais e fiscais do produto."
                : "Cadastre as informações comerciais e fiscais do produto."}
            </p>
        </div>
      </section>

      {erro && (
        <div className="alert alert-danger">
          {erro}
        </div>
      )}

      <form onSubmit={handleSubmit}>
        <section className="card detalhe-card">
          <div className="detalhe-card-titulo">
            <div className="detalhe-card-icone">
              <i className="bi bi-box-seam" />
            </div>

            <div>
              <span className="label-azul">
                PRODUTO
              </span>
              <h3>Dados do produto</h3>
            </div>
          </div>

          <div className="produto-form-grid">
            <div className="produto-campo">
              <label>Código *</label>
              <input
                name="codigo"
                value={form.codigo}
                onChange={alterarCampo}
                maxLength={50}
              />
            </div>

            <div className="produto-campo produto-descricao">
              <label>Descrição *</label>
              <input
                name="descricao"
                value={form.descricao}
                onChange={alterarCampo}
                maxLength={200}
              />
            </div>

            <div className="produto-campo produto-ncm-campo campo-responsivo">
              <label>NCM *</label>

              <input
                type="text"
                value={buscaNcm}
                onChange={alterarBuscaNcm}
                placeholder="Código ou descrição..."
                autoComplete="off"
              />

              {pesquisandoNcm && (
                <small>Pesquisando NCM...</small>
              )}

              {resultadosNcm.length > 0 && (
                <div className="produto-ncm-resultados autocomplete-resultados">
                  {resultadosNcm.map((ncm) => (
                    <button
                      key={ncm.codigo}
                      type="button"
                      onClick={() => selecionarNcm(ncm)}
                    >
                      <strong>{ncm.codigo}</strong>
                      <span>{ncm.descricao}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>

            <div className="produto-campo">
              <label>Unidade *</label>
              <select
                name="unidade"
                value={form.unidade}
                onChange={alterarCampo}
              >
                <option value="UN">UN - Unidade</option>
                <option value="KG">KG - Quilograma</option>
                <option value="G">G - Grama</option>
                <option value="L">L - Litro</option>
                <option value="ML">ML - Mililitro</option>
                <option value="CX">CX - Caixa</option>
                <option value="PCT">PCT - Pacote</option>
              </select>
            </div>

            <div className="produto-campo">
              <label>Valor de venda *</label>
              <input
                name="valorVenda"
                value={form.valorVenda}
                onChange={alterarCampo}
                inputMode="decimal"
                placeholder="0,00"
              />
            </div>
          </div>
        </section>

        <section className="card detalhe-card produto-tributacao-card">
          <div className="detalhe-card-titulo">
            <div className="detalhe-card-icone">
              <i className="bi bi-receipt-cutoff" />
            </div>

            <div>
              <span className="label-azul">
                FISCAL
              </span>
              <h3>Tributação</h3>
            </div>
          </div>

          <div className="produto-campo">
            <label>Configuração tributária *</label>

            <select
              name="configuracaoTributariaId"
              value={form.configuracaoTributariaId}
              onChange={alterarCampo}
            >
              <option value="">
                Selecione...
              </option>

              {configuracoes.map((config) => (
                <option
                  key={config.id}
                  value={config.id}
                >
                  {config.nome} - CFOP {config.cfop}
                </option>
              ))}
            </select>
          </div>
        </section>

        <div className="produto-form-acoes">
          <button
            type="button"
            className="detalhe-btn-secundario"
            onClick={() =>
              navigate("/cadastros/produtos")
            }
          >
            Cancelar
          </button>

          <button
            type="submit"
            className="btn-novo-produto"
            disabled={salvando}
          >
            <i className="bi bi-check-lg" />
            {salvando
            ? "Salvando..."
            : editando
                ? "Salvar alterações"
                : "Salvar produto"}
          </button>
        </div>
      </form>
    </main>
  );
}