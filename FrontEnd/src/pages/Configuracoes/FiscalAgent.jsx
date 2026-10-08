
import { useCallback, useEffect, useState } from "react";
import {
  obterMeusAgentes,
  gerarCodigoVinculacao,
} from "../../services/agenteFiscalService";

export default function FiscalAgent() {
  const [agentes, setAgentes] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");

  const [modalVinculacaoAberto, setModalVinculacaoAberto] =
    useState(false);
  const [gerandoCodigo, setGerandoCodigo] = useState(false);
  const [codigoVinculacao, setCodigoVinculacao] = useState(null);
  const [erroVinculacao, setErroVinculacao] = useState("");
  const [codigoCopiado, setCodigoCopiado] = useState(false);

  const carregarAgentes = useCallback(async () => {
    try {
      setErro("");

      const dados = await obterMeusAgentes();

      setAgentes(dados);
    } catch (error) {
      setErro(
        error.message || "Erro ao consultar os agentes."
      );
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => {
    carregarAgentes();

    const intervalo = setInterval(
      carregarAgentes,
      30000
    );

    return () => clearInterval(intervalo);
  }, [carregarAgentes]);

  function formatarData(data) {
    if (!data) return "Nunca";

    const dataConvertida = new Date(data);

    if (Number.isNaN(dataConvertida.getTime())) {
      return "-";
    }

    return dataConvertida.toLocaleString("pt-BR");
  }

  async function abrirVinculacao() {
    setModalVinculacaoAberto(true);
    setGerandoCodigo(true);
    setCodigoVinculacao(null);
    setErroVinculacao("");
    setCodigoCopiado(false);

    try {
      const dados = await gerarCodigoVinculacao();

      setCodigoVinculacao(dados);
    } catch (error) {
      setErroVinculacao(
        error.message ||
        "Não foi possível gerar o código de vinculação."
      );
    } finally {
      setGerandoCodigo(false);
    }
  }

  function fecharVinculacao() {
    if (gerandoCodigo) return;

    setModalVinculacaoAberto(false);
    setCodigoVinculacao(null);
    setErroVinculacao("");
    setCodigoCopiado(false);

    carregarAgentes();
  }

  async function copiarCodigo() {
    if (!codigoVinculacao?.codigo) return;

    try {
      await navigator.clipboard.writeText(
        String(codigoVinculacao.codigo)
      );

      setCodigoCopiado(true);
      setErroVinculacao("");
    } catch {
      setErroVinculacao(
        "Não foi possível copiar o código."
      );
    }
  }

  const quantidadeOnline = agentes.filter(
    (agente) => agente.ativo && agente.online
  ).length;

  return (
    <>
      <section className="configuracoes-card">
        <div className="configuracoes-card-titulo configuracoes-tributarias-titulo">
          <div className="configuracoes-card-titulo-info">
            <div className="configuracoes-card-icone">
              <i className="bi bi-pc-display" />
            </div>

            <div>
              <h2>Fiscal.Agent</h2>

              <p>
                Computadores autorizados a executar operações
                fiscais utilizando certificados locais.
              </p>
            </div>
          </div>

          <div className="agente-cabecalho-acoes">
            <span className="agente-resumo">
              {quantidadeOnline} online
            </span>

            <button
              type="button"
              className="agente-vincular-botao"
              onClick={abrirVinculacao}
            >
              <i className="bi bi-plus-lg" />
              Vincular computador
            </button>
          </div>
        </div>

        {erro && (
          <div className="configuracoes-mensagem configuracoes-mensagem-erro">
            <i className="bi bi-exclamation-circle" />
            {erro}
          </div>
        )}

        {carregando ? (
          <div className="configuracoes-tributarias-carregando">
            <i className="bi bi-arrow-repeat" />
            Carregando computadores...
          </div>
        ) : agentes.length === 0 ? (
          <div className="configuracoes-tributarias-vazio">
            Nenhum computador vinculado.
          </div>
        ) : (
          <div className="agente-lista">
            {agentes.map((agente) => {
              const online =
                agente.ativo && agente.online;

              return (
                <div
                  className="agente-item"
                  key={agente.id}
                >
                  <div className="agente-item-info">
                    <i className="bi bi-pc-display" />

                    <div>
                      <strong>{agente.nome}</strong>

                      <small>
                        {agente.identificadorMaquina}
                      </small>

                      <small>
                        Última comunicação:{" "}
                        {formatarData(
                          agente.ultimaComunicacaoEm
                        )}
                      </small>
                    </div>
                  </div>

                  <span
                    className={
                      !agente.ativo
                        ? "agente-status agente-status-inativo"
                        : online
                          ? "agente-status agente-status-online"
                          : "agente-status agente-status-offline"
                    }
                  >
                    {!agente.ativo
                      ? "Inativo"
                      : online
                        ? "Online"
                        : "Offline"}
                  </span>
                </div>
              );
            })}
          </div>
        )}
      </section>

      {modalVinculacaoAberto && (
        <div
          className="configuracoes-modal-overlay"
          onMouseDown={(event) => {
            if (
              event.target === event.currentTarget &&
              !gerandoCodigo
            ) {
              fecharVinculacao();
            }
          }}
        >
          <div
            className="configuracoes-confirmacao agente-modal"
            role="dialog"
            aria-modal="true"
            aria-label="Vincular computador"
          >
            <div className="configuracoes-confirmacao-icone">
              <i className="bi bi-pc-display" />
            </div>

            <h2>Vincular computador</h2>

            <p>
              Informe este código no Fiscal.Agent instalado
              no computador que deseja vincular.
            </p>

            {gerandoCodigo ? (
              <div className="agente-codigo-container">
                <i className="bi bi-arrow-repeat" />
                <span>Gerando código de vinculação...</span>
              </div>
            ) : codigoVinculacao ? (
              <div className="agente-codigo-container">
                <strong className="agente-codigo">
                  {codigoVinculacao.codigo}
                </strong>

                <small>
                  Válido até:{" "}
                  {formatarData(
                    codigoVinculacao.expiraEm
                  )}
                </small>

                <button
                  type="button"
                  className="agente-copiar-botao"
                  onClick={copiarCodigo}
                >
                  <i
                    className={
                      codigoCopiado
                        ? "bi bi-check-lg"
                        : "bi bi-clipboard"
                    }
                  />

                  {codigoCopiado
                    ? "Código copiado"
                    : "Copiar código"}
                </button>
              </div>
            ) : null}

            {erroVinculacao && (
              <div className="configuracoes-confirmacao-erro">
                <i className="bi bi-exclamation-circle" />
                <span>{erroVinculacao}</span>
              </div>
            )}

            <p className="agente-modal-aviso">
              <i className="bi bi-shield-check" />

              O certificado digital e sua senha
              permanecerão armazenados no computador
              do cliente.
            </p>

            <div className="configuracoes-confirmacao-acoes">
              <button
                type="button"
                className="configuracoes-modal-cancelar"
                onClick={fecharVinculacao}
                disabled={gerandoCodigo}
              >
                Fechar
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
