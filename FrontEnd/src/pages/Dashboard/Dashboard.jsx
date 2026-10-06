import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { listarNotasFiscais } from "../../services/notaFiscalService";
import "./Dashboard.css";

export default function Dashboard() {
  const navigate = useNavigate();

  const [notas, setNotas] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  /* ============================================================
     CARREGAR DASHBOARD
     ============================================================ */

  useEffect(() => {
    carregarDashboard();
  }, []);

  async function carregarDashboard() {
    try {
      setCarregando(true);
      setErro("");

      const dados = await listarNotasFiscais();

      setNotas(dados || []);
    } catch (error) {
      console.error("Erro ao carregar dashboard:", error);

      setErro(
        error.message ||
          "Não foi possível carregar os dados do Dashboard."
      );
    } finally {
      setCarregando(false);
    }
  }

  /* ============================================================
     RESUMO DO DIA
     ============================================================ */

  const resumo = useMemo(() => {
    const hoje = new Date();

    const notasHoje = notas.filter((nota) => {
      if (!nota.criadoEm) {
        return false;
      }

      const dataNota = new Date(nota.criadoEm);

      const mesmaData =
        dataNota.getFullYear() === hoje.getFullYear() &&
        dataNota.getMonth() === hoje.getMonth() &&
        dataNota.getDate() === hoje.getDate();

      // Dashboard atual trabalha somente com NFC-e
      return (
        mesmaData &&
        Number(nota.modelo) === 65
      );
    });

    const autorizadas = notasHoje.filter(
      (nota) => nota.status === "AUTORIZADA"
    );

    const rejeitadas = notasHoje.filter(
      (nota) => nota.status === "REJEITADA"
    );

    const faturamento = autorizadas.reduce(
      (total, nota) =>
        total + Number(nota.valorTotal || 0),
      0
    );

    return {
      vendas: autorizadas.length,
      faturamento,
      autorizadas: autorizadas.length,
      rejeitadas: rejeitadas.length,
    };
  }, [notas]);

  /* ============================================================
     ÚLTIMAS NFC-e
     ============================================================ */

  const ultimasNotas = useMemo(() => {
    return [...notas]
      .filter(
        (nota) => Number(nota.modelo) === 65
      )
      .sort(
        (a, b) =>
          new Date(b.criadoEm) -
          new Date(a.criadoEm)
      )
      .slice(0, 5);
  }, [notas]);

  /* ============================================================
     TELA
     ============================================================ */

  return (
    <main className="dashboard-pagina">

      {/* ========================================================
          CABEÇALHO
          ======================================================== */}

      <header className="dashboard-cabecalho">

        <div>
          <span>DASHBOARD</span>

          <h2>Visão geral</h2>

          <p>
            Acompanhe as vendas e a situação das notas fiscais.
          </p>
        </div>

        <button
          type="button"
          className="dashboard-nova-venda"
          onClick={() => navigate("/vendas")}
        >
          <i className="bi bi-cart-plus" />

          Nova venda
        </button>

      </header>

      {/* ========================================================
          ERRO
          ======================================================== */}

      {erro && (
        <div className="vendas-erro">
          <i className="bi bi-exclamation-triangle" />
          {" "}
          {erro}
        </div>
      )}

      {/* ========================================================
          CARDS
          ======================================================== */}

      <section className="dashboard-cards">

        {/* VENDAS HOJE */}

        <div className="dashboard-card">

          <div className="dashboard-card-icone azul">
            <i className="bi bi-cart3" />
          </div>

          <div>
            <span>Vendas hoje</span>

            <strong>
              {carregando
                ? "..."
                : resumo.vendas}
            </strong>

            <small>
              Vendas realizadas
            </small>
          </div>

        </div>

        {/* FATURAMENTO */}

        <div className="dashboard-card">

          <div className="dashboard-card-icone verde">
            <i className="bi bi-currency-dollar" />
          </div>

          <div>
            <span>Faturamento hoje</span>

            <strong>
              {carregando
                ? "..."
                : formatarMoeda(
                    resumo.faturamento
                  )}
            </strong>

            <small>
              Total vendido
            </small>
          </div>

        </div>

        {/* AUTORIZADAS */}

        <div className="dashboard-card">

          <div className="dashboard-card-icone verde">
            <i className="bi bi-check-circle" />
          </div>

          <div>
            <span>NFC-e autorizadas</span>

            <strong>
              {carregando
                ? "..."
                : resumo.autorizadas}
            </strong>

            <small>
              Emitidas hoje
            </small>
          </div>

        </div>

        {/* REJEITADAS */}

        <div className="dashboard-card">

          <div className="dashboard-card-icone vermelho">
            <i className="bi bi-exclamation-triangle" />
          </div>

          <div>
            <span>NFC-e rejeitadas</span>

            <strong>
              {carregando
                ? "..."
                : resumo.rejeitadas}
            </strong>

            <small>
              Rejeitadas hoje
            </small>
          </div>

        </div>

      </section>

      {/* ========================================================
          ÚLTIMAS NFC-e
          ======================================================== */}

      <section className="dashboard-notas">

        {/* CABEÇALHO */}

        <div className="dashboard-painel-titulo">

          <div>
            <span>FISCAL</span>

            <h3>
              Últimas NFC-e
            </h3>
          </div>

          <button
            type="button"
            onClick={() =>
              navigate("/fiscal/notas")
            }
          >
            Ver todas

            <i className="bi bi-arrow-right" />
          </button>

        </div>

        {/* TABELA */}

        <div className="dashboard-tabela">

          <table>

            <thead>
              <tr>
                <th>Número</th>
                <th>Data / Hora</th>
                <th>Valor</th>
                <th>Status</th>
              </tr>
            </thead>

            <tbody>

              {/* CARREGANDO */}

              {carregando && (
                <tr>
                  <td colSpan="4">

                    <div className="dashboard-vazio">

                      <i className="bi bi-arrow-repeat" />

                      <strong>
                        Carregando NFC-e...
                      </strong>

                    </div>

                  </td>
                </tr>
              )}

              {/* SEM NOTAS */}

              {!carregando &&
                ultimasNotas.length === 0 && (
                  <tr>
                    <td colSpan="4">

                      <div className="dashboard-vazio">

                        <i className="bi bi-receipt" />

                        <strong>
                          Nenhuma NFC-e para exibir
                        </strong>

                        <span>
                          As últimas notas emitidas aparecerão aqui.
                        </span>

                      </div>

                    </td>
                  </tr>
                )}

              {/* NOTAS */}

              {!carregando &&
                ultimasNotas.map((nota) => (
                  <tr
                    key={nota.id}
                    className="dashboard-nota-linha"
                    onClick={() =>
                      navigate(
                        `/fiscal/notas/${nota.id}`
                      )
                    }
                  >

                    <td>
                      <strong>
                        {nota.numero}
                      </strong>
                    </td>

                    <td>
                      {formatarData(
                        nota.criadoEm
                      )}
                    </td>

                    <td>
                      <strong>
                        {formatarMoeda(
                          nota.valorTotal
                        )}
                      </strong>
                    </td>

                    <td>

                      <span
                        className={`dashboard-status ${
                          nota.status === "AUTORIZADA"
                            ? "autorizada"
                            : nota.status === "REJEITADA"
                              ? "rejeitada"
                              : "outro"
                        }`}
                      >
                        {nota.status ||
                          "SEM STATUS"}
                      </span>

                    </td>

                  </tr>
                ))}

            </tbody>

          </table>

        </div>

      </section>

    </main>
  );
}

/* ============================================================
   FORMATAR MOEDA
   ============================================================ */

function formatarMoeda(valor) {
  return Number(valor || 0).toLocaleString(
    "pt-BR",
    {
      style: "currency",
      currency: "BRL",
    }
  );
}

/* ============================================================
   FORMATAR DATA
   ============================================================ */

function formatarData(data) {
  if (!data) {
    return "-";
  }

  return new Date(data).toLocaleString(
    "pt-BR"
  );
}