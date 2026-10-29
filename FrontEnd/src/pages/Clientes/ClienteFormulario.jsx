import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { obterUsuario } from "../../services/authService";
import { alterarCliente, cadastrarCliente, obterClientePorId } from "../../services/clienteService";
import "./Clientes.css";

const inicial = { tipoPessoa: "PF", nomeRazaoSocial: "", cpfCnpj: "", inscricaoEstadual: "", email: "", telefone: "", cep: "", logradouro: "", numero: "", complemento: "", bairro: "", cidade: "", codigoMunicipioIbge: "", uf: "", ativo: true };
const apenasDigitos = v => String(v || "").replace(/\D/g, "");

export default function ClienteFormulario() {
  const { id } = useParams();
  const novo = !id;
  const navigate = useNavigate();
  const somenteConsulta = obterUsuario()?.perfil === "Consulta";
  const [cliente, setCliente] = useState(inicial);
  const [carregando, setCarregando] = useState(!novo);
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState("");
  const [sucesso, setSucesso] = useState("");

  useEffect(() => {
    if (novo) return;
    let ativo = true;
    obterClientePorId(id).then(d => { if (ativo) setCliente({ ...inicial, ...d }); })
      .catch(e => { if (ativo) setErro(e.message); })
      .finally(() => { if (ativo) setCarregando(false); });
    return () => { ativo = false; };
  }, [id, novo]);

  const alterar = (campo, valor) => setCliente(c => ({ ...c, [campo]: valor }));
  const campo = (rotulo, nome, props = {}) => <label className="cliente-campo" key={nome}><span>{rotulo}</span><input value={cliente[nome] ?? ""} disabled={somenteConsulta || salvando} onChange={e => alterar(nome, e.target.value)} {...props} /></label>;

  async function salvar(e) {
    e.preventDefault();
    if (somenteConsulta) return;
    setErro(""); setSucesso("");
    const documento = apenasDigitos(cliente.cpfCnpj);
    if (!cliente.nomeRazaoSocial.trim()) return setErro("Informe o nome ou razão social.");
    if (documento.length !== (cliente.tipoPessoa === "PJ" ? 14 : 11)) return setErro("Informe um CPF/CNPJ com a quantidade correta de dígitos.");
    const dados = { ...cliente, cpfCnpj: documento, cep: apenasDigitos(cliente.cep) || null, telefone: apenasDigitos(cliente.telefone) || null, uf: cliente.uf?.toUpperCase() || null };
    setSalvando(true);
    try {
      if (novo) await cadastrarCliente(dados);
      else await alterarCliente(id, dados);
      setSucesso("Cliente salvo com sucesso.");
      navigate("/cadastros/clientes");
    } catch (e) { setErro(e.message || "Erro ao salvar cliente."); }
    finally { setSalvando(false); }
  }

  if (carregando) return <main className="conteudo clientes-pagina">Carregando cliente...</main>;
  return <main className="conteudo clientes-pagina">
    <section className="clientes-cabecalho"><div><span className="label-azul">CADASTROS</span><h2>{novo ? "Novo cliente" : "Dados do cliente"}</h2><p>Preencha os dados para identificação fiscal do destinatário.</p></div></section>
    {erro && <div className="alert alert-danger">{erro}</div>}{sucesso && <div className="alert alert-success">{sucesso}</div>}
    <form onSubmit={salvar}>
      <section className="card cliente-form-card"><h3>Dados principais</h3><div className="cliente-form-grid">
        <label className="cliente-campo"><span>Tipo de pessoa</span><select value={cliente.tipoPessoa} disabled={somenteConsulta || salvando} onChange={e => { alterar("tipoPessoa", e.target.value); }}><option value="PF">Pessoa física</option><option value="PJ">Pessoa jurídica</option></select></label>
        {campo("Nome / Razão social *", "nomeRazaoSocial", { required: true, className: "" })}
        {campo(cliente.tipoPessoa === "PJ" ? "CNPJ *" : "CPF *", "cpfCnpj", { required: true, inputMode: "numeric", maxLength: 18 })}
        {campo("Inscrição estadual", "inscricaoEstadual")}
        {campo("E-mail", "email", { type: "email" })}
        {campo("Telefone", "telefone", { type: "tel" })}
        <label className="cliente-campo"><span>Situação</span><select value={String(cliente.ativo)} disabled={somenteConsulta || salvando} onChange={e => alterar("ativo", e.target.value === "true")}><option value="true">Ativo</option><option value="false">Inativo</option></select></label>
      </div></section>
      <section className="card cliente-form-card"><h3>Endereço</h3><div className="cliente-form-grid">
        {campo("CEP", "cep", { inputMode: "numeric" })}
        {campo("Logradouro", "logradouro")}
        {campo("Número", "numero")}
        {campo("Complemento", "complemento")}
        {campo("Bairro", "bairro")}
        {campo("Cidade", "cidade")}
        {campo("Código IBGE do município", "codigoMunicipioIbge", { inputMode: "numeric" })}
        {campo("UF", "uf", { maxLength: 2 })}
      </div></section>
      <div className="cliente-form-acoes"><button type="button" className="cliente-btn-secundario" onClick={() => navigate("/cadastros/clientes")}>Voltar</button>{!somenteConsulta && <button className="btn-novo-cliente" type="submit" disabled={salvando}>{salvando ? "Salvando..." : "Salvar cliente"}</button>}</div>
    </form>
  </main>;
}
