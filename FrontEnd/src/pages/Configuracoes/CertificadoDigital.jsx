
import { useEffect, useState } from "react";
import { obterMeuCertificadoDigital } from "../../services/certificadoDigitalService";

export default function CertificadoDigital() {
  const [certificado, setCertificado] = useState(null);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");
  const [modalAberto, setModalAberto] = useState(false);

  async function carregarCertificado() {
    try {
      setCarregando(true);
      setErro("");

      const dados = await obterMeuCertificadoDigital();
      setCertificado(dados);
    } catch (error) {
      setErro(
        error.message ||
          "Não foi possível consultar o certificado digital."
      );
    } finally {
      setCarregando(false);
    }
  }

  useEffect(() => {
    carregarCertificado();
  }, []);

  function formatarData(data) {
    if (!data) return "-";

    const valor = new Date(data);

    if (Number.isNaN(valor.getTime())) return "-";

    return valor.toLocaleDateString("pt-BR");
  }

  function obterStatus() {
    if (!certificado) {
      return { texto: "Não configurado", cor: "#6c757d" };
    }

    if (!certificado.ativo) {
      return { texto: "Inativo", cor: "#dc3545" };
    }

    if (!certificado.validoAte) {
      return { texto: "Validade não informada", cor: "#d97706" };
    }

    const vencimento = new Date(certificado.validoAte);

    if (Number.isNaN(vencimento.getTime())) {
      return { texto: "Validade inválida", cor: "#dc3545" };
    }

    const agora = new Date();

    if (vencimento <= agora) {
      return { texto: "Vencido", cor: "#dc3545" };
    }

    const diasRestantes =
      (vencimento.getTime() - agora.getTime()) / 86400000;

    if (diasRestantes <= 30) {
      return {
        texto: "Próximo do vencimento",
        cor: "#d97706",
      };
    }

    return { texto: "Válido", cor: "#198754" };
  }

  const status = obterStatus();

  return (
    <>
      {/* CARD COMPACTO */}
      <section className="configuracoes-card">
        <div className="configuracoes-card-titulo">
          <div className="configuracoes-card-icone">
            <i className="bi bi-shield-lock" />
          </div>

          <div>
            <h2>Certificado Digital</h2>
            <p>
              Certificado A1 utilizado na emissão de documentos fiscais.
            </p>
          </div>
        </div>

        <div
          style={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            gap: 16,
            flexWrap: "wrap",
          }}
        >
          <div>
            {carregando ? (
              <span>Consultando certificado...</span>
            ) : erro ? (
              <span style={{ color: "#dc3545" }}>
                Não foi possível consultar o certificado.
              </span>
            ) : (
              <>
                <strong style={{ color: status.cor }}>
                  <i className="bi bi-circle-fill" />{" "}
                  {status.texto}
                </strong>

                {certificado && (
                  <p style={{ marginTop: 8 }}>
                    Validade: {formatarData(certificado.validoAte)}
                  </p>
                )}
              </>
            )}
          </div>

          <button
            type="button"
            className="configuracoes-salvar"
            onClick={() => setModalAberto(true)}
          >
            <i className="bi bi-gear" /> Configurar
          </button>
        </div>
      </section>

      {/* MODAL */}
      {modalAberto && (
        <div
          className="configuracoes-modal-overlay"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setModalAberto(false);
            }
          }}
        >
          <div
            className="configuracoes-confirmacao"
            role="dialog"
            aria-modal="true"
            aria-labelledby="titulo-modal-certificado"
            style={{
              width: "min(680px, 95vw)",
              maxWidth: "680px",
              maxHeight: "90vh",
              overflowY: "auto",
              textAlign: "left",
            }}
          >
            <div
              style={{
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                gap: 12,
              }}
            >
              <h2 id="titulo-modal-certificado">
                Certificado Digital
              </h2>

              <button
                type="button"
                onClick={() => setModalAberto(false)}
                aria-label="Fechar"
                style={{
                  border: "none",
                  background: "transparent",
                  cursor: "pointer",
                  fontSize: 22,
                }}
              >
                <i className="bi bi-x-lg" />
              </button>
            </div>

            {carregando ? (
              <p>Carregando certificado...</p>
            ) : erro ? (
              <div className="configuracoes-mensagem configuracoes-mensagem-erro">
                {erro}
              </div>
            ) : !certificado ? (
              <p>
                Nenhum certificado digital foi configurado
                para esta empresa.
              </p>
            ) : (
              <>
                <p>
                  <strong>Status: </strong>
                  <span
                    style={{
                      color: status.cor,
                      fontWeight: 600,
                    }}
                  >
                    {status.texto}
                  </span>
                </p>


                <div
                className="configuracoes-grid"
                style={{
                    gridTemplateColumns: "repeat(2, minmax(0, 1fr))",
                }}
                >
                {[
                    ["Titular", certificado.titular],
                    ["CNPJ", certificado.cnpj],
                    ["Emissor", certificado.emissor],
                    ["Válido de", formatarData(certificado.validoDe)],
                    ["Válido até", formatarData(certificado.validoAte)],
                    ["Armazenamento", "Local (Fiscal.Agent)"],
                ].map(([titulo, valor]) => (
                    <div
                    className="configuracoes-campo"
                    key={titulo}
                    style={{
                        gridColumn: titulo === "Titular" ? "1 / -1" : undefined,
                        minWidth: 0,
                    }}
                    >
                    <label>{titulo}</label>
                    <input
                        value={valor || "-"}
                        readOnly
                        style={{
                        width: "100%",
                        boxSizing: "border-box",
                        }}
                    />
                    </div>
                ))}
                </div>


                <p style={{ marginTop: 16 }}>
                  <small>
                    O arquivo PFX e sua senha permanecem no
                    computador. Para substituir o certificado,
                    utilize o Fiscal.Certificado.Manager e
                    reinicie o Agent para sincronizar os novos dados.
                  </small>
                </p>
              </>
            )}

            <div className="configuracoes-confirmacao-acoes">
              <button
                type="button"
                className="configuracoes-modal-cancelar"
                onClick={() => setModalAberto(false)}
              >
                Fechar
              </button>

              <button
                type="button"
                className="configuracoes-salvar"
                onClick={carregarCertificado}
                disabled={carregando}
              >
                <i className="bi bi-arrow-clockwise" /> Atualizar
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
