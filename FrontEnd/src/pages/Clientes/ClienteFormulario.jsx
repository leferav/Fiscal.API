import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { obterUsuario } from "../../services/authService";
import { alterarCliente, cadastrarCliente, obterClientePorId } from "../../services/clienteService";
import { somenteNumeros, validarCPF, validarCNPJ, validarEmail, validarCEP, validarTelefone } from "../../utils/validacoes";
import { mascaraCPF, mascaraCNPJ, mascaraCEP, mascaraTelefone } from "../../utils/mascaras";
import "./Clientes.css";

const inicial = { tipoPessoa: "PF", nomeRazaoSocial: "", cpfCnpj: "", inscricaoEstadual: "", email: "", telefone: "", cep: "", logradouro: "", numero: "", complemento: "", bairro: "", cidade: "", codigoMunicipioIbge: "", uf: "", ativo: true };
const estados = ["AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"];

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
  const [errosCampos, setErrosCampos] = useState({});

  useEffect(() => {
    if (novo) return;
    let ativo = true;
    obterClientePorId(id)
      .then(d => {
        if (!ativo) return;
        setCliente({
          ...inicial, ...d,
          cpfCnpj: d.tipoPessoa === "PJ" ? mascaraCNPJ(d.cpfCnpj) : mascaraCPF(d.cpfCnpj),
          telefone: mascaraTelefone(d.telefone),
          cep: mascaraCEP(d.cep),
        });
      })
      .catch(e => { if (ativo) setErro(e.message || "Não foi possível carregar o cliente."); })
      .finally(() => { if (ativo) setCarregando(false); });
    return () => { ativo = false; };
  }, [id, novo]);

  function alterar(nome, valor) {
    let novoValor = valor;
    if (nome === "cpfCnpj") novoValor = cliente.tipoPessoa === "PJ" ? mascaraCNPJ(valor) : mascaraCPF(valor);
    if (nome === "telefone") novoValor = mascaraTelefone(valor);
    if (nome === "cep") novoValor = mascaraCEP(valor);
    if (nome === "uf") novoValor = String(valor).toUpperCase().slice(0, 2);
    if (nome === "codigoMunicipioIbge") novoValor = somenteNumeros(valor).slice(0, 7);
    setCliente(c => ({ ...c, [nome]: novoValor }));
    setErrosCampos(c => { const copia = { ...c }; delete copia[nome]; return copia; });
  }

  function validarFormulario() {
    const erros = {};
    const nome = String(cliente.nomeRazaoSocial || "").trim();
    const documento = somenteNumeros(cliente.cpfCnpj);
    if (!nome) erros.nomeRazaoSocial = "Informe o nome ou razão social.";
    if (cliente.tipoPessoa === "PJ") {
      if (!validarCNPJ(documento)) erros.cpfCnpj = "CNPJ inválido. Verifique os 14 dígitos e os dígitos verificadores.";
    } else if (!validarCPF(documento)) {
      erros.cpfCnpj = "CPF inválido. Verifique os 11 dígitos e os dígitos verificadores.";
    }
    if (cliente.email?.trim() && !validarEmail(cliente.email)) erros.email = "Informe um e-mail válido.";
    if (cliente.telefone?.trim() && !validarTelefone(cliente.telefone)) erros.telefone = "Telefone inválido. Informe DDD e número com 10 ou 11 dígitos.";
    if (cliente.cep?.trim() && !validarCEP(cliente.cep)) erros.cep = "CEP inválido. Informe 8 dígitos.";
    if (cliente.uf?.trim() && !estados.includes(cliente.uf.trim().toUpperCase())) erros.uf = "Informe uma UF válida.";
    if (cliente.codigoMunicipioIbge?.trim() && !/^\d{7}$/.test(cliente.codigoMunicipioIbge.trim())) erros.codigoMunicipioIbge = "O código IBGE deve ter 7 dígitos.";
    return erros;
  }

  function campo(rotulo, nome, props = {}) {
    return <label className="cliente-campo" key={nome}>
      <span>{rotulo}</span>
      <input
        {...props}
        value={cliente[nome] ?? ""}
        disabled={somenteConsulta || salvando}
        onChange={e => alterar(nome, e.target.value)}
        aria-invalid={Boolean(errosCampos[nome])}
        style={errosCampos[nome] ? { borderColor: "#dc3545" } : undefined}
      />
      {errosCampos[nome] && <small role="alert" style={{ color: "#b42332", fontSize: "12px" }}>{errosCampos[nome]}</small>}
    </label>;
  }

  async function salvar(e) {
    e.preventDefault();
    if (somenteConsulta) return;
    setErro(""); setSucesso("");
    const erros = validarFormulario();
    setErrosCampos(erros);
    if (Object.keys(erros).length) return;

    const dados = {
      ...cliente,
      nomeRazaoSocial: cliente.nomeRazaoSocial.trim(),
      cpfCnpj: somenteNumeros(cliente.cpfCnpj),
      email: cliente.email?.trim() || null,
      cep: somenteNumeros(cliente.cep) || null,
      telefone: somenteNumeros(cliente.telefone) || null,
      uf: cliente.uf?.trim().toUpperCase() || null,
    };
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
    {erro && <div className="alert alert-danger" role="alert">{erro}</div>}
    {sucesso && <div className="alert alert-success">{sucesso}</div>}
    <form onSubmit={salvar} noValidate>
      <section className="card cliente-form-card"><h3>Dados principais</h3><div className="cliente-form-grid">
        <label className="cliente-campo"><span>Tipo de pessoa</span><select value={cliente.tipoPessoa} disabled={somenteConsulta || salvando} onChange={e => { setCliente(c => ({ ...c, tipoPessoa: e.target.value, cpfCnpj: "" })); setErrosCampos(c => { const copia = { ...c }; delete copia.cpfCnpj; return copia; }); }}><option value="PF">Pessoa física</option><option value="PJ">Pessoa jurídica</option></select></label>
        {campo("Nome / Razão social *", "nomeRazaoSocial", { required: true })}
        {campo(cliente.tipoPessoa === "PJ" ? "CNPJ *" : "CPF *", "cpfCnpj", { required: true, inputMode: "numeric", maxLength: cliente.tipoPessoa === "PJ" ? 18 : 14, placeholder: cliente.tipoPessoa === "PJ" ? "00.000.000/0000-00" : "000.000.000-00" })}
        {campo("Inscrição estadual", "inscricaoEstadual")}
        {campo("E-mail", "email", { type: "email", placeholder: "email@exemplo.com" })}
        {campo("Telefone", "telefone", { type: "tel", placeholder: "(00) 00000-0000" })}
        <label className="cliente-campo"><span>Situação</span><select value={String(cliente.ativo)} disabled={somenteConsulta || salvando} onChange={e => alterar("ativo", e.target.value === "true")}><option value="true">Ativo</option><option value="false">Inativo</option></select></label>
      </div></section>
      <section className="card cliente-form-card"><h3>Endereço</h3><div className="cliente-form-grid">
        {campo("CEP", "cep", { inputMode: "numeric", placeholder: "00000-000", maxLength: 9 })}
        {campo("Logradouro", "logradouro")}
        {campo("Número", "numero")}
        {campo("Complemento", "complemento")}
        {campo("Bairro", "bairro")}
        {campo("Cidade", "cidade")}
        {campo("Código IBGE do município", "codigoMunicipioIbge", { inputMode: "numeric", maxLength: 7 })}
        {campo("UF", "uf", { maxLength: 2, placeholder: "MG" })}
      </div></section>
      {Object.keys(errosCampos).length > 0 && <div role="alert" style={{ color: "#b42332", marginBottom: "12px" }}>Revise os campos destacados acima antes de salvar.</div>}
      <div className="cliente-form-acoes"><button type="button" className="cliente-btn-secundario" onClick={() => navigate("/cadastros/clientes")}>Voltar</button>{!somenteConsulta && <button className="btn-novo-cliente" type="submit" disabled={salvando}>{salvando ? "Salvando..." : "Salvar cliente"}</button>}</div>
    </form>
  </main>;
}
