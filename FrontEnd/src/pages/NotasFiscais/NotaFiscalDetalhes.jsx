import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { obterNotaFiscalPorId, baixarXmlNotaFiscal,   baixarDanfeNotaFiscal} from "../../services/notaFiscalService";
import "./NotasFiscais.css";
import "./NotaFiscalDetalhes.css";

export default function NotaFiscalDetalhes() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [nota, setNota] = useState(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  useEffect(() => {
    async function carregarNota() {
      try {
        setCarregando(true);
        setErro("");

        const dados = await obterNotaFiscalPorId(id);

        setNota(dados);
      } catch (error) {
        console.error(
          "Erro ao carregar nota fiscal:",
          error
        );

        setErro(
          error.message ||
            "Não foi possível carregar a nota fiscal."
        );
      } finally {
        setCarregando(false);
      }
    }

    carregarNota();
  }, [id]);

  if (carregando) {
    return (
      <main className="conteudo">
        <div className="card">
          <div style={{ padding: "24px" }}>
            Carregando nota fiscal...
          </div>
        </div>
      </main>
    );
  }

  if (erro) {
    return (
      <main className="conteudo">
        <div className="card">
          <div style={{ padding: "24px" }}>
            <div className="alert alert-danger">
              {erro}
            </div>

            <button
              type="button"
              className="btn btn-outline-primary"
              onClick={() => navigate("/fiscal/notas")}
            >
              Voltar
            </button>
          </div>
        </div>
      </main>
    );
  }

  async function handleBaixarXml() {
    try {
      const blob = await baixarXmlNotaFiscal(id);

      const url = window.URL.createObjectURL(blob);

      const link = document.createElement("a");

      link.href = url;
      link.download =
        nota.modelo === 65
          ? `NFCe-${nota.numero}.xml`
          : `NFe-${nota.numero}.xml`;

      document.body.appendChild(link);

      link.click();
      link.remove();

      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error(
        "Erro ao baixar XML:",
        error
      );

      alert(
        error.message ||
          "Não foi possível baixar o XML."
      );
    }
  }

  async function handleBaixarDanfe() {
    try {
      const blob = await baixarDanfeNotaFiscal(id);

      const url = window.URL.createObjectURL(blob);

      const link = document.createElement("a");

      link.href = url;
      link.download = `NFCe-${nota.numero}.pdf`;

      document.body.appendChild(link);

      link.click();
      link.remove();

      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error(
        "Erro ao baixar DANFE:",
        error
      );

      alert(
        error.message ||
          "Não foi possível baixar o DANFE."
      );
    }
  } 

  return (
    <main className="conteudo detalhe-nota-pagina">

      <button
        type="button"
        className="detalhe-voltar"
        onClick={() => navigate("/fiscal/notas")}
      >
        <i className="bi bi-arrow-left" />
        Voltar para Notas Fiscais
      </button>

      <section className="detalhe-cabecalho">
        <div>
          <span className="label-azul">
            DOCUMENTO FISCAL
          </span>

          <div className="detalhe-titulo">
            <h2>
              {nota.modelo === 65 ? "NFC-e" : "NF-e"}{" "}
              {nota.numero}
            </h2>

            <StatusBadge status={nota.status} />
          </div>

          <p>
            Série {nota.serie}
            <span>•</span>
            {nota.ambiente === 1
              ? "Produção"
              : "Homologação"}
          </p>
        </div>
      </section>

      <section className="card detalhe-card">
        <div className="detalhe-card-titulo">
          <div className="detalhe-card-icone">
            <i className="bi bi-file-earmark-text" />
          </div>

          <div>
            <span className="label-azul">
              IDENTIFICAÇÃO
            </span>
            <h3>Dados do documento</h3>
          </div>
        </div>

        <div className="detalhe-chave">
          <span>Chave de acesso</span>

          <div className="detalhe-chave-conteudo">
            <strong>
              {nota.chaveAcesso || "-"}
            </strong>

            {nota.chaveAcesso && (
              <button
                type="button"
                onClick={() =>
                  navigator.clipboard.writeText(
                    nota.chaveAcesso
                  )
                }
              >
                <i className="bi bi-copy" />
                Copiar
              </button>
            )}
          </div>
        </div>

        <div className="detalhe-grid-info">
          <Info titulo="Número" valor={nota.numero} />
          <Info titulo="Série" valor={nota.serie} />

          <Info
            titulo="Modelo"
            valor={
              nota.modelo === 65
                ? "65 - NFC-e"
                : "55 - NF-e"
            }
          />

          <Info
            titulo="Ambiente"
            valor={
              nota.ambiente === 1
                ? "Produção"
                : "Homologação"
            }
          />

          <Info
            titulo="Protocolo"
            valor={nota.protocolo || "-"}
          />

          <Info
            titulo="cStat"
            valor={nota.cStat ?? "-"}
          />
        </div>
      </section>

      <div className="detalhe-duas-colunas">

        <section className="card detalhe-card">
          <div className="detalhe-card-titulo">
            <div className="detalhe-card-icone">
              <i className="bi bi-cash-stack" />
            </div>

            <div>
              <span className="label-azul">
                VALORES
              </span>
              <h3>Totais da nota</h3>
            </div>
          </div>

          <div className="detalhe-valores">
            <div>
              <span>Valor dos produtos</span>
              <strong>
                {formatarMoeda(nota.valorProdutos)}
              </strong>
            </div>

            <div className="detalhe-total">
              <span>Valor total</span>
              <strong>
                {formatarMoeda(nota.valorTotal)}
              </strong>
            </div>
          </div>
        </section>

        <section className="card detalhe-card">
          <div className="detalhe-card-titulo">
            <div className="detalhe-card-icone">
              <i className="bi bi-calendar3" />
            </div>

            <div>
              <span className="label-azul">
                DATAS
              </span>
              <h3>Processamento</h3>
            </div>
          </div>

          <div className="detalhe-datas">
            <Info
              titulo="Emitida em"
              valor={formatarData(nota.criadoEm)}
            />

            <Info
              titulo="Autorizada em"
              valor={formatarData(nota.autorizadoEm)}
            />

            {nota.atualizadoEm && (
              <Info
                titulo="Última atualização"
                valor={formatarData(nota.atualizadoEm)}
              />
            )}
          </div>
        </section>

      </div>

      {/* Retorno da autorização original, preservado mesmo após cancelamento */}
      <section className="card detalhe-sefaz sucesso">
        <div className="detalhe-sefaz-icone">
          <i className="bi bi-check-circle" />
        </div>
        <div>
          <span>RETORNO SEFAZ — AUTORIZAÇÃO</span>
          <strong>{nota.cStat ?? "-"} - {nota.xMotivo || "Sem retorno informado"}</strong>
        </div>
      </section>

      {(nota.status === "CANCELADA" ||
        nota.status === "CANCELAMENTO_INDETERMINADO" ||
        nota.status === "CANCELAMENTO_EM_ANDAMENTO") && (() => {

        const cancelada = nota.status === "CANCELADA";

        const indeterminado =
          nota.status === "CANCELAMENTO_INDETERMINADO" ||
          nota.status === "CANCELAMENTO_EM_ANDAMENTO";

        const rejeitado = !cancelada && !indeterminado;

        const corFundo = cancelada
          ? "#fee2e2"
          : "#fff7ed";

        const corTexto = cancelada
          ? "#991b1b"
          : "#9a3412";

        const corBorda = cancelada
          ? "#ef4444"
          : "#f59e0b";

        return (
          <section
            className="card detalhe-sefaz"
            style={{
              marginTop: 16,
              backgroundColor: corFundo,
              color: corTexto,
              borderLeft: `4px solid ${corBorda}`
            }}
          >
            <div
              className="detalhe-sefaz-icone"
              style={{ color: corTexto }}
            >
              <i
                className={`bi ${
                  cancelada
                    ? "bi-check-circle"
                    : indeterminado
                      ? "bi-exclamation-triangle"
                      : "bi-info-circle"
                }`}
              />
            </div>

            <div>
              <span>
                {cancelada
                  ? "CANCELAMENTO CONFIRMADO PELA SEFAZ"
                  : indeterminado
                    ? "CANCELAMENTO PENDENTE DE VERIFICAÇÃO"
                    : "TENTATIVA DE CANCELAMENTO REJEITADA"}
              </span>

              <strong>
                {nota.cStatCancelamento ?? "-"} -{" "}
                {nota.motivoCancelamento ||
                  "Situação do cancelamento não confirmada"}
              </strong>

              {rejeitado && (
                <div style={{ marginTop: 8 }}>
                  A NFC-e permanece autorizada.
                  O cancelamento não foi realizado.
                </div>
              )}

              {indeterminado && (
                <div style={{ marginTop: 8 }}>
                  Não foi possível confirmar o resultado do cancelamento.
                  Consulte a situação do evento antes de tentar novamente.
                </div>
              )}

              {nota.protocoloCancelamento && (
                <div style={{ marginTop: 8 }}>
                  Protocolo do cancelamento:{" "}
                  <strong>{nota.protocoloCancelamento}</strong>
                </div>
              )}

              {nota.canceladoEm && (
                <div style={{ marginTop: 4 }}>
                  Cancelada em: {formatarData(nota.canceladoEm)}
                </div>
              )}
            </div>
          </section>
        );
      })()}

      <section className="detalhe-acoes">
        <button
          type="button"
          className="detalhe-btn-secundario"
          onClick={handleBaixarXml}
          disabled={!(["AUTORIZADA", "CANCELADA"].includes(nota.status))}
          title={
            nota.status === "AUTORIZADA"
              ? "Baixar XML autorizado"
              : "XML disponível somente para notas autorizadas"
          }
        >
          <i className="bi bi-filetype-xml" />
          Baixar XML
        </button>

        <button
          type="button"
          className="detalhe-btn-secundario"
          onClick={handleBaixarDanfe}
          disabled={
            nota.status !== "AUTORIZADA" ||
            nota.modelo !== 65
          }
          title={
            nota.status !== "AUTORIZADA"
              ? "DANFE disponível somente para notas autorizadas"
              : nota.modelo !== 65
                ? "DANFE NFC-e disponível somente para modelo 65"
                : "Baixar DANFE NFC-e"
          }
        >
          <i className="bi bi-printer" />
          DANFE
        </button>
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

function Info({ titulo, valor }) {
  return (
    <div className="detalhe-info">
      <span>{titulo}</span>
      <strong>{valor}</strong>
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
    <span className={`detalhe-status ${classe}`}>
      {status || "SEM STATUS"}
    </span>
  );
}

function formatarData(data) {
  if (!data) return "-";

  return new Date(data).toLocaleString("pt-BR");
}