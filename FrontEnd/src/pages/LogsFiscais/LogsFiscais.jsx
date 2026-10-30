import { useEffect, useState } from "react";
import { obterEmpresaTrabalho, obterUsuario } from "../../services/authService";
import { listarLogsFiscais, obterLogFiscal } from "../../services/logsFiscaisService";
import "./LogsFiscais.css";

const paginaTamanho = 30;
const fmtData = valor => valor ? new Date(valor).toLocaleString("pt-BR") : "-";
const dataUtc = (valor, fim = false) => valor ? new Date(`${valor}T${fim ? "23:59:59.999" : "00:00:00"}`).toISOString() : undefined;

export default function LogsFiscais() {
  const perfil = obterUsuario()?.perfil;
  const empresa = obterEmpresaTrabalho();
  const [filtros, setFiltros] = useState({ empresaId: empresa?.id || "", numero: "", inicio: "", fim: "", evento: "", cStat: "" });
  const [pagina, setPagina] = useState(1);
  const [resultado, setResultado] = useState({ registros: [], total: 0 });
  const [detalhe, setDetalhe] = useState(null);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState("");
  const [carregandoDetalhe, setCarregandoDetalhe] = useState(false);
  const [versaoBusca, setVersaoBusca] = useState(0);

  useEffect(() => {
    let ativo = true;
    setCarregando(true); setErro("");
    const params = { pagina, tamanhoPagina: paginaTamanho, ...filtros };
    if (params.inicio) params.inicio = dataUtc(params.inicio);
    if (params.fim) params.fim = dataUtc(params.fim, true);
    listarLogsFiscais(params).then(dados => {
      if (ativo) setResultado({ registros: dados?.registros || [], total: dados?.total || 0 });
    }).catch(e => { if (ativo) setErro(e.message); })
      .finally(() => { if (ativo) setCarregando(false); });
    return () => { ativo = false; };
  }, [pagina, versaoBusca]);

  async function abrirDetalhe(id) {
    setErro(""); setCarregandoDetalhe(true); setDetalhe(null);
    try { setDetalhe(await obterLogFiscal(id)); }
    catch (e) { setErro(e.message); }
    finally { setCarregandoDetalhe(false); }
  }
  function atualizar(campo, valor) { setFiltros(atual => ({ ...atual, [campo]: valor })); }
  function buscar(e) { e.preventDefault(); setPagina(1); setVersaoBusca(v => v + 1); }
  const paginas = Math.max(1, Math.ceil(resultado.total / paginaTamanho));
  return (
    <main className="conteudo logs-pagina">
      <header className="logs-cabecalho"><span className="label-azul">FISCAL</span><h2>Monitoramento Fiscal</h2><p>Histórico de emissão e ocorrências da SEFAZ.</p></header>
      <section className="card logs-card">
        <form className="logs-filtros" onSubmit={buscar}>
          <label>Empresa ID<input value={filtros.empresaId} onChange={e => atualizar("empresaId", e.target.value)} disabled={perfil !== "Master"} placeholder="ID da empresa" /></label>
          <label>Número da nota<input type="number" min="1" value={filtros.numero} onChange={e => atualizar("numero", e.target.value)} placeholder="Ex.: 900091" /></label>
          <label>Data inicial<input type="date" value={filtros.inicio} onChange={e => atualizar("inicio", e.target.value)} /></label>
          <label>Data final<input type="date" value={filtros.fim} onChange={e => atualizar("fim", e.target.value)} /></label>
          <label>Evento<input value={filtros.evento} onChange={e => atualizar("evento", e.target.value)} placeholder="NFCe_REJEITADA" /></label>
          <label>cStat<input type="number" value={filtros.cStat} onChange={e => atualizar("cStat", e.target.value)} placeholder="100" /></label>
          <button type="submit" disabled={carregando}><i className="bi bi-search" /> Pesquisar</button>
        </form>
        {erro && <div className="alert alert-danger m-3" role="alert">{erro}</div>}
        <div className="logs-contador">{carregando ? "Consultando..." : `${resultado.total} evento(s) encontrado(s)`}</div>
        <div className="table-responsive"><table className="logs-tabela"><thead><tr><th>Data/hora</th><th>Empresa</th><th>Nota</th><th>Evento</th><th>Nível</th><th>cStat</th><th>Mensagem</th><th>Ação</th></tr></thead><tbody>
          {resultado.registros.map(log => <tr key={log.id}><td>{fmtData(log.criadoEm)}</td><td title={log.empresaId}>{String(log.empresaId || "-").slice(0, 8)}...</td><td>{log.numero || "-"}</td><td>{log.evento}</td><td>{log.nivel}</td><td>{log.cStat ?? "-"}</td><td className="logs-mensagem">{log.mensagem || "-"}</td><td><button type="button" onClick={() => abrirDetalhe(log.id)}>Detalhes</button></td></tr>)}
          {!carregando && !resultado.registros.length && <tr><td colSpan="8" className="logs-vazio">Nenhum log encontrado.</td></tr>}
        </tbody></table></div>
        <div className="logs-paginacao"><button disabled={pagina <= 1 || carregando} onClick={() => setPagina(p => p - 1)}>Anterior</button><span>Página {pagina} de {paginas}</span><button disabled={pagina >= paginas || carregando} onClick={() => setPagina(p => p + 1)}>Próxima</button></div>
      </section>
      {(detalhe || carregandoDetalhe) && <div className="logs-modal-fundo" onClick={() => setDetalhe(null)}><section className="logs-modal" role="dialog" aria-modal="true" aria-label="Detalhes do log fiscal" onClick={e => e.stopPropagation()}><header><h3>Detalhes do evento</h3><button onClick={() => setDetalhe(null)} aria-label="Fechar">×</button></header>{carregandoDetalhe ? <p>Carregando...</p> : <dl>{Object.entries(detalhe || {}).map(([chave, valor]) => <div key={chave}><dt>{chave}</dt><dd>{valor === null ? "-" : String(valor)}</dd></div>)}</dl>}</section></div>}
    </main>
  );
}
