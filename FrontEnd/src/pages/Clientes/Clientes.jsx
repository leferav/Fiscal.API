import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { obterUsuario } from "../../services/authService";
import { obterClientes } from "../../services/clienteService";
import "./Clientes.css";

function formatarDocumento(valor) {
  const d = String(valor || "").replace(/\D/g, "");
  if (d.length === 11) return d.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, "$1.$2.$3-$4");
  if (d.length === 14) return d.replace(/(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, "$1.$2.$3/$4-$5");
  return valor || "-";
}

export default function Clientes() {
  const [clientes, setClientes] = useState([]);
  const [pesquisa, setPesquisa] = useState("");
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");
  const navigate = useNavigate();
  const somenteConsulta = obterUsuario()?.perfil === "Consulta";

  useEffect(() => {
    let ativo = true;
    async function carregar() {
      try {
        const dados = await obterClientes();
        if (ativo) setClientes(Array.isArray(dados) ? dados : dados?.clientes || dados?.itens || []);
      } catch (e) {
        if (ativo) setErro(e.message || "Não foi possível carregar os clientes.");
      } finally {
        if (ativo) setCarregando(false);
      }
    }
    carregar();
    return () => { ativo = false; };
  }, []);

  const filtrados = useMemo(() => {
    const termo = pesquisa.trim().toLowerCase();
    const numeros = termo.replace(/\D/g, "");
    return clientes.filter(c => !termo ||
      [c.nomeRazaoSocial, c.cpfCnpj, c.email, c.telefone].some(v => String(v || "").toLowerCase().includes(termo)) ||
      (numeros && String(c.cpfCnpj || "").includes(numeros)));
  }, [clientes, pesquisa]);

  return (
    <main className="conteudo clientes-pagina">
      <section className="clientes-cabecalho">
        <div><span className="label-azul">CADASTROS</span><h2>Clientes</h2><p>Gerencie os destinatários dos documentos fiscais.</p></div>
        {!somenteConsulta && <button className="btn-novo-cliente" onClick={() => navigate("/cadastros/clientes/novo")}><i className="bi bi-plus-lg" /> Novo cliente</button>}
      </section>
      <section className="card clientes-card">
        <div className="clientes-filtros"><div className="clientes-pesquisa"><i className="bi bi-search" /><input value={pesquisa} onChange={e => setPesquisa(e.target.value)} placeholder="Pesquisar por nome, CPF/CNPJ, e-mail ou telefone..." /></div></div>
        {erro && <div className="alert alert-danger m-3">{erro}</div>}
        {carregando ? <div className="clientes-vazio">Carregando clientes...</div> :
          <div className="table-responsive"><table className="clientes-tabela"><thead><tr><th>Nome / Razão social</th><th>CPF / CNPJ</th><th>Tipo</th><th>Telefone</th><th>Situação</th><th>Ações</th></tr></thead><tbody>
            {filtrados.map(c => <tr key={c.id}><td><strong>{c.nomeRazaoSocial}</strong></td><td>{formatarDocumento(c.cpfCnpj)}</td><td>{c.tipoPessoa === "PJ" ? "Jurídica" : "Física"}</td><td>{c.telefone || "-"}</td><td><span className={`cliente-status ${c.ativo ? "ativo" : "inativo"}`}>{c.ativo ? "Ativo" : "Inativo"}</span></td><td><button type="button" className="cliente-btn-acao" title="Visualizar cliente" onClick={() => navigate(`/cadastros/clientes/${c.id}`)}><i className="bi bi-eye" /></button></td></tr>)}
            {!filtrados.length && <tr><td colSpan="6" className="clientes-vazio">Nenhum cliente encontrado.</td></tr>}
          </tbody></table></div>}
      </section>
    </main>
  );
}
