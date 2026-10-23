import { useEffect, useMemo, useState } from "react";
import { listarUsuarios, alterarStatusUsuario } from "../../services/usuariosService";
import { obterUsuario } from "../../services/authService";
import { listarEmpresas } from "../../services/empresaService";
import UsuarioFormulario from "./UsuarioFormulario";
import "./Usuarios.css";

export default function Usuarios() {
  const [usuarios, setUsuarios] = useState([]);
  const [pesquisa, setPesquisa] = useState("");
  const [empresas, setEmpresas] = useState([]);
  const [empresaFiltro, setEmpresaFiltro] = useState("");
  const [erroEmpresas, setErroEmpresas] = useState("");
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");
  const [sucesso, setSucesso] = useState("");
  const [modalAberto, setModalAberto] = useState(false);
  const [usuarioEdicao, setUsuarioEdicao] = useState(null);
  const [usuarioStatus, setUsuarioStatus] = useState(null);
  const [alterandoStatus, setAlterandoStatus] = useState(false);

  const usuarioLogado = obterUsuario();
  const master = usuarioLogado?.perfil === "Master";

  useEffect(() => {
    if (!master) return;
    let ativo = true;
    listarEmpresas()
      .then((dados) => {
        if (ativo) setEmpresas(Array.isArray(dados) ? dados : []);
      })
      .catch((error) => {
        if (ativo) setErroEmpresas(error.message || "Não foi possível carregar as empresas.");
      });
    return () => { ativo = false; };
  }, [master]);

  useEffect(() => {
    let montado = true;

    async function carregar() {
      try {
        const dados = await listarUsuarios();
        if (montado) setUsuarios(dados);
      } catch (error) {
        if (montado) setErro(error.message || "Não foi possível carregar os usuários.");
      } finally {
        if (montado) setCarregando(false);
      }
    }

    carregar();
    return () => { montado = false; };
  }, []);

  const usuariosFiltrados = useMemo(() => {
    const termo = pesquisa.trim().toLocaleLowerCase("pt-BR");
    return usuarios.filter((usuario) =>
      (!master || !empresaFiltro || usuario.empresaId === empresaFiltro) &&
      [usuario.nome, usuario.email, usuario.perfil, usuario.empresa]
        .some((valor) => String(valor ?? "").toLocaleLowerCase("pt-BR").includes(termo))
    );
  }, [usuarios, pesquisa, master, empresaFiltro]);

  const usuariosDaEmpresa = useMemo(() => usuarios.filter((usuario) =>
    !master || !empresaFiltro || usuario.empresaId === empresaFiltro
  ), [usuarios, master, empresaFiltro]);
  const totalAtivos = usuariosDaEmpresa.filter((usuario) => usuario.ativo).length;
  const totalInativos = usuariosDaEmpresa.length - totalAtivos;

  function fecharFormulario() {
    setModalAberto(false);
    setUsuarioEdicao(null);
  }

  async function salvarComSucesso() {
    const editou = Boolean(usuarioEdicao);
    fecharFormulario();
    setErro("");
    setSucesso(editou ? "Usuário atualizado com sucesso." : "Usuário cadastrado com sucesso.");

    try {
      setUsuarios(await listarUsuarios());
    } catch (error) {
      setErro(error.message || "Alteração salva, mas não foi possível atualizar a listagem.");
    }
  }

  function podeEditar(usuario) {
    if (usuario.perfil === "Master" || usuario.id === usuarioLogado?.id) return false;
    if (master) return Boolean(usuario.empresaId);
    return usuarioLogado?.perfil === "Administrador" && usuario.perfil !== "Administrador";
  }

  function podeAlterarStatus(usuario) {
    if (usuario.perfil === "Administrador" || usuario.perfil === "Master") return false;
    if (usuario.id === usuarioLogado?.id) return false;
    if (master) return Boolean(usuario.empresaId);
    return usuarioLogado?.perfil === "Administrador";
  }

  async function confirmarStatus() {
    if (!usuarioStatus || alterandoStatus || !podeAlterarStatus(usuarioStatus)) return;
    setAlterandoStatus(true);
    setErro("");
    setSucesso("");
    try {
      await alterarStatusUsuario(
        usuarioStatus.id,
        !usuarioStatus.ativo,
        master ? usuarioStatus.empresaId : null
      );
      setUsuarioStatus(null);
      setSucesso(usuarioStatus.ativo ? "Usuário desativado com sucesso." : "Usuário ativado com sucesso.");
      try {
        setUsuarios(await listarUsuarios());
      } catch (error) {
        setErro(error.message || "Status alterado, mas não foi possível atualizar a listagem.");
      }
    } catch (error) {
      setErro(error.message || "Não foi possível alterar o status do usuário.");
    } finally {
      setAlterandoStatus(false);
    }
  }

  return (
    <div className="usuarios-page">
      <div className="usuarios-header">
        <div>
          <h1>Gerenciamento de Usuários</h1>
          <p>Cadastre e gerencie os acessos da empresa.</p>
        </div>

        <button
          type="button"
          className="usuarios-btn-novo"
          onClick={() => { setUsuarioEdicao(null); setModalAberto(true); setSucesso(""); }}
          disabled={master && empresas.length === 0}
          title={master && empresas.length === 0 ? "Aguardando lista de empresas" : "Novo usuário"}
        >
          <i className="bi bi-plus-lg" /> Novo usuário
        </button>
      </div>

      <div className="usuarios-resumo">
        <div className="usuarios-indicador"><span>Total de usuários</span><strong>{usuariosDaEmpresa.length}</strong></div>
        <div className="usuarios-indicador"><span>Ativos</span><strong>{totalAtivos}</strong></div>
        <div className="usuarios-indicador"><span>Inativos</span><strong>{totalInativos}</strong></div>
      </div>

      {sucesso && <div className="usuarios-sucesso" role="status"><i className="bi bi-check-circle" /> {sucesso}</div>}

      <div className="usuarios-content">
        <div className="usuarios-filtros" style={{ display: "flex", flexWrap: "wrap", gap: 12, alignItems: "center" }}>
          {master && (
            <select
              aria-label="Filtrar por empresa"
              value={empresaFiltro}
              onChange={(e) => setEmpresaFiltro(e.target.value)}
              style={{ minWidth: 260, maxWidth: "100%", padding: "11px 12px", border: "1px solid #cbd8e8", borderRadius: 6, background: "white", color: "#17365d" }}
            >
              <option value="">Todas as empresas</option>
              {empresas.map((empresa) => (
                <option key={empresa.id} value={empresa.id}>
                  {empresa.nomeFantasia || empresa.razaoSocial || empresa.cnpj || empresa.id}
                  {empresa.ativo === false ? " (Inativa)" : ""}
                </option>
              ))}
            </select>
          )}
          <div className="usuarios-pesquisa">
            <i className="bi bi-search" />
            <input
              type="search"
              placeholder="Pesquisar nome, e-mail ou perfil..."
              value={pesquisa}
              onChange={(e) => setPesquisa(e.target.value)}
            />
          </div>
        </div>

        {erroEmpresas && <div className="usuarios-erro" role="alert">{erroEmpresas}</div>}
        {erro && <div className="usuarios-erro" role="alert">{erro}</div>}

        {carregando ? (
          <p className="usuarios-mensagem">Carregando usuários...</p>
        ) : (
          <div className="usuarios-tabela-container">
            <table className="usuarios-tabela">
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>E-mail</th>
                  {master && <th>Empresa</th>}
                  <th>Perfil</th>
                  <th>Status</th>
                  <th>Ações</th>
                </tr>
              </thead>
              <tbody>
                {usuariosFiltrados.map((usuario) => (
                  <tr key={`${usuario.id}-${usuario.empresaId ?? "sem-empresa"}`}>
                    <td>{usuario.nome}</td>
                    <td>{usuario.email}</td>
                    {master && <td>{usuario.empresa}</td>}
                    <td>{usuario.perfil}</td>
                    <td>
                      <span className={`usuarios-status ${usuario.ativo ? "ativo" : "inativo"}`}>
                        {usuario.ativo ? "Ativo" : "Inativo"}
                      </span>
                    </td>
                    <td>
                      <div className="usuarios-acoes">
                        <button
                          type="button"
                          className="usuarios-btn-acao"
                          aria-label={`Editar ${usuario.nome}`}
                          title={podeEditar(usuario) ? "Editar usuário" : "Edição não permitida"}
                          disabled={!podeEditar(usuario)}
                          onClick={() => { setUsuarioEdicao(usuario); setSucesso(""); }}
                        >
                          <i className="bi bi-pencil-square" />
                        </button>
                        <button
                          type="button"
                          className={`usuarios-btn-acao ${usuario.ativo ? "usuarios-acao-desativar" : "usuarios-acao-ativar"}`}
                          aria-label={`${usuario.ativo ? "Desativar" : "Ativar"} ${usuario.nome}`}
                          title={podeAlterarStatus(usuario) ? (usuario.ativo ? "Desativar usuário" : "Ativar usuário") : "Alteração de status não permitida"}
                          disabled={!podeAlterarStatus(usuario) || alterandoStatus}
                          onClick={() => { setUsuarioStatus(usuario); setErro(""); setSucesso(""); }}
                        >
                          <i className="bi bi-power" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {usuariosFiltrados.length === 0 && (
                  <tr><td colSpan={master ? 6 : 5} className="usuarios-vazio">Nenhum usuário encontrado.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {usuarioStatus && (
        <div className="usuarios-confirmacao-overlay">
          <div className="usuarios-confirmacao" role="dialog" aria-modal="true" aria-labelledby="usuarios-confirmacao-titulo">
            <h2 id="usuarios-confirmacao-titulo">
              {usuarioStatus.ativo ? "Desativar usuário" : "Ativar usuário"}
            </h2>
            <p>
              Confirma que deseja {usuarioStatus.ativo ? "desativar" : "ativar"} o acesso de
              {" "}<strong>{usuarioStatus.nome}</strong>?
            </p>
            <div className="usuarios-confirmacao-botoes">
              <button type="button" className="usuarios-confirmacao-cancelar" disabled={alterandoStatus} onClick={() => setUsuarioStatus(null)}>
                Cancelar
              </button>
              <button type="button" className={usuarioStatus.ativo ? "usuarios-confirmacao-desativar" : "usuarios-confirmacao-ativar"} disabled={alterandoStatus} onClick={confirmarStatus}>
                {alterandoStatus ? "Processando..." : usuarioStatus.ativo ? "Desativar" : "Ativar"}
              </button>
            </div>
          </div>
        </div>
      )}

      {(modalAberto || usuarioEdicao) && (
        <UsuarioFormulario
          key={usuarioEdicao ? `${usuarioEdicao.id}-${usuarioEdicao.empresaId ?? ""}` : "novo"}
          usuario={usuarioEdicao}
          master={master}
          empresas={empresas}
          empresaIdInicial={master ? (usuarioEdicao?.empresaId || empresaFiltro) : ""}
          onFechar={fecharFormulario}
          onSucesso={salvarComSucesso}
        />
      )}
    </div>
  );
}
