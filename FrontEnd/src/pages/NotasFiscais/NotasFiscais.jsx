import { useEffect, useMemo, useState } from "react";
import { listarNotasFiscais } from "../../services/notaFiscalService";
import { useNavigate } from "react-router-dom";
import "./NotasFiscais.css";

export default function NotasFiscais() {
  const navigate = useNavigate();

  const [notas, setNotas] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  const [pesquisa, setPesquisa] = useState("");
  const [status, setStatus] = useState("TODOS");
  const [modelo, setModelo] = useState("TODOS");

  useEffect(() => {
    carregarNotas();
  }, []);

  async function carregarNotas() {
    try {
      setCarregando(true);
      setErro("");

      const dados = await listarNotasFiscais();
      setNotas(dados);
    } catch (error) {
      console.error("Erro ao carregar notas fiscais:", error);

      setErro(
        error.message ||
          "Não foi possível carregar as notas fiscais."
      );
    } finally {
      setCarregando(false);
    }
  }

  const resumo = useMemo(() => {
    const autorizadas = notas.filter(
      (nota) => nota.status === "AUTORIZADA"
    );

    const rejeitadas = notas.filter(
      (nota) => nota.status === "REJEITADA"
    );

    const valorAutorizado = autorizadas.reduce(
      (total, nota) =>
        total + Number(nota.valorTotal || 0),
      0
    );

    return {
      total: notas.length,
      autorizadas: autorizadas.length,
      rejeitadas: rejeitadas.length,
      valorAutorizado,
    };
  }, [notas]);

  const notasFiltradas = useMemo(() => {
    const termo = pesquisa
      .trim()
      .toLowerCase();

    return notas.filter((nota) => {
      const correspondePesquisa =
        !termo ||
        String(nota.numero ?? "")
          .toLowerCase()
          .includes(termo);

      const correspondeStatus =
        status === "TODOS" ||
        nota.status === status;

      const correspondeModelo =
        modelo === "TODOS" ||
        String(nota.modelo) === modelo;

      return (
        correspondePesquisa &&
        correspondeStatus &&
        correspondeModelo
      );
    });
  }, [notas, pesquisa, status, modelo]);

  return (
    <main className="conteudo notas-pagina">

      {/* TÍTULO */}
      <div className="notas-cabecalho-pagina">
        <div>
          <span className="label-azul">
            FISCAL
          </span>

          <h2>Notas Fiscais</h2>

          <p>
            Consulte e gerencie os documentos fiscais emitidos.
          </p>
        </div>

        <button
          type="button"
          className="notas-btn-atualizar"
          onClick={carregarNotas}
          disabled={carregando}
        >
          <i className="bi bi-arrow-clockwise" />
          Atualizar
        </button>
      </div>

      {/* RESUMO */}
      <section className="notas-resumo">
        <CardResumo
          icone="bi bi-files"
          titulo="Total de notas"
          valor={resumo.total}
        />

        <CardResumo
          icone="bi bi-check-circle"
          titulo="Autorizadas"
          valor={resumo.autorizadas}
          tipo="sucesso"
        />

        <CardResumo
          icone="bi bi-x-circle"
          titulo="Rejeitadas"
          valor={resumo.rejeitadas}
          tipo="erro"
        />

        <CardResumo
          icone="bi bi-cash-stack"
          titulo="Valor autorizado"
          valor={formatarMoeda(
            resumo.valorAutorizado
          )}
          tipo="valor"
        />
      </section>

      {/* FILTROS */}
      <section className="card notas-filtros">
        <div className="notas-filtros-titulo">
          <div className="notas-filtros-icone">
            <i className="bi bi-funnel" />
          </div>

          <div>
            <span className="label-azul">
              CONSULTA
            </span>
            <h3>Localizar documentos</h3>
          </div>
        </div>

        <div className="notas-filtros-campos">
          <div className="notas-pesquisa">
            <i className="bi bi-search" />

            <input
              type="text"
              className="form-control"
              placeholder="Número ou chave de acesso..."
              value={pesquisa}
              onChange={(e) =>
                setPesquisa(e.target.value)
              }
            />
          </div>

          <select
            className="form-select notas-select"
            value={modelo}
            onChange={(e) =>
              setModelo(e.target.value)
            }
          >
            <option value="TODOS">
              Todos os modelos
            </option>
            <option value="65">NFC-e</option>
            <option value="55">NF-e</option>
          </select>

          <select
            className="form-select notas-select"
            value={status}
            onChange={(e) =>
              setStatus(e.target.value)
            }
          >
            <option value="TODOS">
              Todos os status
            </option>
            <option value="AUTORIZADA">
              Autorizadas
            </option>
            <option value="REJEITADA">
              Rejeitadas
            </option>
          </select>
        </div>
      </section>

      {/* TABELA */}
      <section className="card notas-card-tabela">
        <div className="notas-tabela-cabecalho">
          <div>
            <span className="label-azul">
              DOCUMENTOS
            </span>

            <h3>Notas emitidas</h3>
          </div>

          <span className="notas-quantidade">
            {notasFiltradas.length} registro(s)
          </span>
        </div>

        {carregando && (
          <div className="notas-mensagem">
            <i className="bi bi-arrow-repeat" />
            Carregando notas fiscais...
          </div>
        )}

        {erro && (
          <div className="notas-erro">
            {erro}
          </div>
        )}

        {!carregando &&
          !erro &&
          notasFiltradas.length === 0 && (
            <div className="notas-mensagem">
              <i className="bi bi-file-earmark-x" />
              Nenhuma nota fiscal encontrada.
            </div>
          )}

        {!carregando &&
          !erro &&
          notasFiltradas.length > 0 && (
            <div className="table-responsive notas-tabela">
              <table className="table mb-0">
                <thead>
                  <tr>
                    <th>Número</th>
                    <th>Modelo</th>
                    <th>Emissão</th>
                    <th>Status</th>
                    <th>Valor</th>
                    <th>cStat</th>
                    <th>Ações</th>
                  </tr>
                </thead>

                <tbody>
                  {notasFiltradas.map((nota) => (
                    <tr key={nota.id}>
                      <td>
                        <div className="notas-numero">
                          {nota.numero}
                        </div>

                        <small>
                          Série {nota.serie}
                        </small>
                      </td>

                      <td>
                        {obterModelo(nota.modelo)}
                      </td>

                      <td>
                        {formatarData(
                          nota.criadoEm
                        )}
                      </td>

                      <td>
                        <StatusBadge
                          status={nota.status}
                        />
                      </td>

                      <td className="notas-valor">
                        {formatarMoeda(
                          nota.valorTotal
                        )}
                      </td>

                      <td>
                        {nota.cStat ?? "-"}
                      </td>

                      <td>
                        <button
                        type="button"
                        className="notas-btn-acao"
                        title="Visualizar nota"
                        onClick={() =>
                            navigate(`/fiscal/notas/${nota.id}`)
                        }
                        >
                        <i className="bi bi-eye" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
      </section>
    </main>
  );
}

function CardResumo({
  icone,
  titulo,
  valor,
  tipo = "",
}) {
  return (
    <div className={`notas-card-resumo ${tipo}`}>
      <div className="notas-card-icone">
        <i className={icone} />
      </div>

      <div>
        <span>{titulo}</span>
        <strong>{valor}</strong>
      </div>
    </div>
  );
}

function StatusBadge({ status }) {
  const classe =
    status === "AUTORIZADA"
      ? "autorizada"
      : status === "REJEITADA"
        ? "rejeitada"
        : "outro";

  return (
    <span
      className={`notas-status ${classe}`}
    >
      {status || "SEM STATUS"}
    </span>
  );
}

function obterModelo(modelo) {
  if (modelo === 65) return "NFC-e";
  if (modelo === 55) return "NF-e";

  return modelo ?? "-";
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

function formatarData(data) {
  if (!data) return "-";

  return new Date(data).toLocaleString(
    "pt-BR"
  );
}