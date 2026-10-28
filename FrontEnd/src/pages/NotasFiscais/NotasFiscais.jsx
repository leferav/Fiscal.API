import { useEffect, useMemo, useRef, useState } from "react";
import { listarNotasFiscais, listarVendasPendentes, emitirVendaPendente, cancelarNotaFiscal } from "../../services/notaFiscalService";
import { useNavigate } from "react-router-dom";
import "./NotasFiscais.css";

export default function NotasFiscais() {
  const navigate = useNavigate();
  const emissaoEmAndamento = useRef(false);
  const cancelamentoEmAndamento = useRef(false);
  const [notaParaCancelar, setNotaParaCancelar] = useState(null);
  const [justificativaCancelamento, setJustificativaCancelamento] = useState("");
  const [cancelandoId, setCancelandoId] = useState(null);
  const [avisoCancelamento, setAvisoCancelamento] = useState("");
  const [vendaEmitindoId, setVendaEmitindoId] = useState(null);
  const [avisoEmissao, setAvisoEmissao] = useState("");
  const [notas, setNotas] = useState([]);
  const [vendasPendentes, setVendasPendentes] = useState([]);
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

      const [dadosNotas, dadosVendas] = await Promise.all([
        listarNotasFiscais(),
        listarVendasPendentes(),
      ]);
      setNotas(dadosNotas);
      setVendasPendentes(dadosVendas);
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

  async function confirmarEmissao(venda) {
    if (emissaoEmAndamento.current || carregando) return;
    if (!window.confirm(
      `Transmitir a venda ${venda.id.slice(0, 8)} (${formatarMoeda(venda.valorTotal)}) à SEFAZ?\n\nNão repita a operação se houver falha de comunicação.`
    )) return;

    emissaoEmAndamento.current = true;
    setVendaEmitindoId(venda.id);
    setAvisoEmissao("");
    setErro("");

    try {
      const resultado = await emitirVendaPendente(venda.id);
      if (resultado?.sucesso) {
        setAvisoEmissao(`NFC-e ${resultado.numero} autorizada. Protocolo: ${resultado.protocolo || "não informado"}.`);
      } else {
        setAvisoEmissao(
          `A SEFAZ retornou cStat ${resultado?.cStat ?? "não informado"}. ` +
          `Situação: ${resultado?.motivo || "consulte o registro fiscal antes de tentar novamente"}.`
        );
      }
    } catch (error) {
      setErro(
        `${error.message || "Falha ao emitir venda."} ` +
        "Verifique a situação da venda e da NFC-e antes de qualquer nova tentativa."
      );
    } finally {
      // Atualiza inclusive em caso de timeout: a venda pode ter sido transmitida.
      try {
        await carregarNotas();
      } catch (error) {
        console.error("Erro ao atualizar a listagem:", error);
      }
      emissaoEmAndamento.current = false;
      setVendaEmitindoId(null);
    }
  }

  function abrirCancelamento(nota) {
    if (cancelamentoEmAndamento.current || emissaoEmAndamento.current) return;
    setNotaParaCancelar(nota);
    setJustificativaCancelamento("");
    setErro("");
    setAvisoCancelamento("");
  }

  async function confirmarCancelamento(event) {
    event.preventDefault();
    if (!notaParaCancelar || cancelamentoEmAndamento.current) return;
    const justificativa = justificativaCancelamento.trim();
    if (justificativa.length < 15 || justificativa.length > 255) {
      setErro("A justificativa deve ter entre 15 e 255 caracteres.");
      return;
    }
    if (!window.confirm(`Enviar à SEFAZ o cancelamento da NFC-e ${notaParaCancelar.numero}?\n\nNão repita a operação se houver falha de comunicação.`)) return;

    const id = notaParaCancelar.id;
    cancelamentoEmAndamento.current = true;
    setCancelandoId(id);
    setErro("");
    setAvisoCancelamento("");
    try {
      const resultado = await cancelarNotaFiscal(id, justificativa);
      if (resultado?.confirmado) {
        setAvisoCancelamento(`NFC-e ${notaParaCancelar.numero} cancelada pela SEFAZ. Protocolo: ${resultado.protocoloCancelamento || "não informado"}.`);
      } else {
        setAvisoCancelamento(`Cancelamento não confirmado pela SEFAZ. cStat ${resultado?.cStatEvento ?? "não informado"}: ${resultado?.motivo || "resultado inconclusivo"}. Situação: ${resultado?.status || "verifique a nota"}.`);
      }
      setNotaParaCancelar(null);
      setJustificativaCancelamento("");
    } catch (error) {
      setErro(`${error.message || "Falha ao solicitar cancelamento."} Consulte a situação da NFC-e antes de tentar novamente.`);
      setNotaParaCancelar(null);
    } finally {
      try { await carregarNotas(); } catch (error) { console.error("Erro ao atualizar notas:", error); }
      cancelamentoEmAndamento.current = false;
      setCancelandoId(null);
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

  const documentos = useMemo(() => [
    ...notas.map((nota) => ({ ...nota, tipoRegistro: "NOTA" })),
    ...vendasPendentes.map((venda) => ({
      ...venda,
      tipoRegistro: "VENDA",
      modelo: 65,
      numero: null,
      serie: null,
      cStat: null,
    })),
  ].sort((a, b) => new Date(b.criadoEm) - new Date(a.criadoEm)), [notas, vendasPendentes]);

  const notasFiltradas = useMemo(() => {
    const termo = pesquisa
      .trim()
      .toLowerCase();

    return documentos.filter((nota) => {
      const correspondePesquisa =
        !termo ||
        String(nota.numero ?? (nota.tipoRegistro === "VENDA" ? nota.id : ""))
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
  }, [documentos, pesquisa, status, modelo]);

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
          disabled={carregando || vendaEmitindoId !== null || cancelandoId !== null}
        >
          <i className="bi bi-arrow-clockwise" />
          Atualizar
        </button>
      </div>

      {avisoCancelamento && (
        <div role="status" className="alert alert-info">{avisoCancelamento}</div>
      )}

      {avisoEmissao && (
        <div role="status" className="alert alert-info">{avisoEmissao}</div>
      )}

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
            <option value="CANCELADA">Canceladas</option>
            <option value="CANCELAMENTO_INDETERMINADO">Cancelamento indeterminado</option>
            <option value="A_ENVIAR">
              A Enviar
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

            <h3>Notas fiscais e vendas pendentes</h3>
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
                    <tr
                      key={`${nota.tipoRegistro}-${nota.id}`}
                      className={
                        nota.tipoRegistro === "VENDA"
                          ? "notas-linha-pendente"
                          : ""
                      }
                    >
                      <td>
                        <div className="notas-numero">
                          {nota.tipoRegistro === "VENDA" ? "Venda pendente" : nota.numero}
                        </div>

                        <small>
                          {nota.tipoRegistro === "VENDA" ? `Ref. ${nota.id.slice(0, 8)}` : `Série ${nota.serie}`}
                        </small>
                      </td>

                      <td>
                        {nota.tipoRegistro === "VENDA" ? "NFC-e (a emitir)" : obterModelo(nota.modelo)}
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
                        {nota.tipoRegistro === "NOTA" ? (
                        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
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
                        {nota.modelo === 65 && nota.status === "AUTORIZADA" && (
                          <button
                            type="button"
                            onClick={() => abrirCancelamento(nota)}
                            disabled={carregando || vendaEmitindoId !== null || cancelandoId !== null}
                            title="Solicitar cancelamento à SEFAZ"
                            style={{ background: "#e06363", color: "#fff", border: 0, borderRadius: 6, padding: "8px 12px", cursor: "pointer" }}
                          >
                            <i className="bi bi-x-circle" />
                          </button>
                        )}
                        </div>
                        ) : (
                          <button
                            type="button"
                            disabled={carregando || vendaEmitindoId !== null || cancelandoId !== null}
                            onClick={() => confirmarEmissao(nota)}
                            title="Emitir NFC-e desta venda pendente"
                            style={{ background: "#48a9e6", color: "white", border: 0, borderRadius: 6, padding: "8px 12px", opacity: vendaEmitindoId !== null ? 0.65 : 1, cursor: vendaEmitindoId !== null ? "wait" : "pointer" }}
                          >
                            <i className={`bi ${vendaEmitindoId === nota.id ? "bi-arrow-repeat" : "bi-send"}`} /> {vendaEmitindoId === nota.id ? "Emitindo..." : "A Enviar"}
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
      </section>

      {notaParaCancelar && (
        <div role="presentation" style={{ position: "fixed", inset: 0, background: "rgba(0,0,0,.58)", zIndex: 1100, display: "flex", alignItems: "center", justifyContent: "center", padding: 16 }}>
          <form onSubmit={confirmarCancelamento} role="dialog" aria-modal="true" aria-labelledby="titulo-cancelamento" style={{ background: "#fff", color: "#222", borderRadius: 12, padding: 24, width: "100%", maxWidth: 520, boxShadow: "0 12px 40px rgba(0,0,0,.25)" }}>
            <h3 id="titulo-cancelamento">Cancelar NFC-e {notaParaCancelar.numero}</h3>
            <p>O pedido será enviado à SEFAZ. A nota só será cancelada se o evento for aceito.</p>
            <label htmlFor="justificativa-cancelamento" style={{ display: "block", fontWeight: 600, marginBottom: 8 }}>Justificativa (15 a 255 caracteres)</label>
            <textarea
              id="justificativa-cancelamento"
              className="form-control"
              rows={4}
              maxLength={255}
              minLength={15}
              required
              autoFocus
              value={justificativaCancelamento}
              onChange={(e) => setJustificativaCancelamento(e.target.value)}
              disabled={cancelandoId !== null}
            />
            <small>{justificativaCancelamento.trim().length}/255 caracteres</small>
            <div style={{ display: "flex", gap: 10, justifyContent: "flex-end", marginTop: 20 }}>
              <button type="button" className="btn btn-outline-secondary" disabled={cancelandoId !== null} onClick={() => { setNotaParaCancelar(null); setJustificativaCancelamento(""); }}>Voltar</button>
              <button type="submit" className="btn btn-danger" disabled={cancelandoId !== null || justificativaCancelamento.trim().length < 15 || justificativaCancelamento.trim().length > 255}>
                {cancelandoId !== null ? "Consultando SEFAZ..." : "Confirmar cancelamento"}
              </button>
            </div>
          </form>
        </div>
      )}
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
      : status === "CANCELADA"
        ? "cancelada"
        : status === "REJEITADA"
        ? "rejeitada"
        : status === "A_ENVIAR"
          ? "a-enviar"
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