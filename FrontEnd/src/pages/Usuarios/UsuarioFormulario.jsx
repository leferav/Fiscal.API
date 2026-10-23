import { useState } from "react";
import {
  cadastrarUsuario,
  atualizarUsuario
} from "../../services/usuariosService";
import "./UsuarioFormulario.css";

export default function UsuarioFormulario({
  usuario = null,
  master = false,
  empresas = [],
  empresaIdInicial = "",
  onFechar,
  onSucesso
}) {
  const editando = Boolean(usuario);
  const [form, setForm] = useState(() => ({
    nome: usuario?.nome ?? "",
    email: usuario?.email ?? "",
    senha: "",
    confirmarSenha: "",
    perfil: usuario?.perfil ?? "Operador",
    empresaId: usuario?.empresaId ?? empresaIdInicial
  }));
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState("");
  const [mostrarSenha, setMostrarSenha] = useState(false);

  function alterarCampo(event) {
    const { name, value } = event.target;
    setForm((anterior) => ({ ...anterior, [name]: value }));
    setErro("");
  }

  async function salvar(event) {
    event.preventDefault();
    if (salvando) return;

    if (!form.nome.trim() || !form.email.trim()) {
      setErro("Nome e e-mail são obrigatórios.");
      return;
    }

    if (master && !form.empresaId) {
      setErro("Selecione a empresa.");
      return;
    }
    if (master && !editando && empresas.find((e) => e.id === form.empresaId)?.ativo === false) {
      setErro("Não é permitido cadastrar usuários em empresas inativas.");
      return;
    }

    if (!editando) {
      if (form.senha !== form.confirmarSenha) {
        setErro("As senhas informadas não coincidem.");
        return;
      }
      if (form.senha.length < 8) {
        setErro("A senha deve possuir pelo menos 8 caracteres.");
        return;
      }
    }

    try {
      setSalvando(true);
      setErro("");
      const dados = {
        nome: form.nome.trim(),
        email: form.email.trim(),
        perfil: form.perfil,
        ...(master ? { empresaId: form.empresaId } : {})
      };

      if (editando) {
        await atualizarUsuario(usuario.id, dados);
      } else {
        await cadastrarUsuario({ ...dados, senha: form.senha });
      }

      await onSucesso?.();
    } catch (error) {
      setErro(error?.message || "Não foi possível salvar o usuário.");
    } finally {
      setSalvando(false);
    }
  }

  return (
    <div className="usuario-modal-overlay">
      <div
        className="usuario-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="usuario-modal-titulo"
      >
        <div className="usuario-modal-header">
          <div>
            <h2 id="usuario-modal-titulo">
              {editando ? "Editar usuário" : "Novo usuário"}
            </h2>
            <p>
              {editando
                ? "Atualize os dados do usuário."
                : "Preencha os dados para cadastrar um usuário."}
            </p>
          </div>
          <button
            type="button"
            className="usuario-modal-fechar"
            onClick={onFechar}
            disabled={salvando}
            aria-label="Fechar"
          >
            <i className="bi bi-x-lg" />
          </button>
        </div>

        <form onSubmit={salvar}>
          <div className="usuario-modal-body">
            {erro && (
              <div className="usuario-form-erro" role="alert">
                <i className="bi bi-exclamation-circle" />
                {erro}
              </div>
            )}

            {master && (
              <div className="usuario-form-campo">
                <label htmlFor="usuario-empresa">Empresa *</label>
                <select
                  id="usuario-empresa"
                  name="empresaId"
                  value={form.empresaId}
                  onChange={alterarCampo}
                  disabled={editando || salvando}
                  required
                >
                  <option value="">Selecione a empresa</option>
                  {empresas.map((empresa) => (
                    <option key={empresa.id} value={empresa.id} disabled={!editando && empresa.ativo === false}>
                      {empresa.nomeFantasia || empresa.razaoSocial || empresa.cnpj || empresa.id}
                      {empresa.ativo === false ? " (Inativa)" : ""}
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div className="usuario-form-campo">
              <label htmlFor="usuario-nome">Nome completo *</label>
              <input
                id="usuario-nome"
                name="nome"
                value={form.nome}
                onChange={alterarCampo}
                placeholder="Nome completo"
                autoComplete="name"
                required
              />
            </div>

            <div className="usuario-form-campo">
              <label htmlFor="usuario-email">E-mail *</label>
              <input
                id="usuario-email"
                type="email"
                name="email"
                value={form.email}
                onChange={alterarCampo}
                placeholder="usuario@empresa.com"
                autoComplete="off"
                required
              />
            </div>

            {!editando && (
              <div className="usuario-form-grid">
                <div className="usuario-form-campo">
                  <label htmlFor="usuario-senha">Senha *</label>
                  <div className="usuario-senha-container">
                    <input
                      id="usuario-senha"
                      type={mostrarSenha ? "text" : "password"}
                      name="senha"
                      value={form.senha}
                      onChange={alterarCampo}
                      autoComplete="new-password"
                      minLength={8}
                      required
                    />
                    <button
                      type="button"
                      onClick={() => setMostrarSenha((valor) => !valor)}
                      aria-label={mostrarSenha ? "Ocultar senha" : "Mostrar senha"}
                    >
                      <i className={mostrarSenha ? "bi bi-eye-slash" : "bi bi-eye"} />
                    </button>
                  </div>
                </div>
                <div className="usuario-form-campo">
                  <label htmlFor="usuario-confirmar">Confirmar senha *</label>
                  <input
                    id="usuario-confirmar"
                    type={mostrarSenha ? "text" : "password"}
                    name="confirmarSenha"
                    value={form.confirmarSenha}
                    onChange={alterarCampo}
                    autoComplete="new-password"
                    required
                  />
                </div>
              </div>
            )}

            <div className="usuario-form-campo">
              <label htmlFor="usuario-perfil">Perfil de acesso *</label>
              <select
                id="usuario-perfil"
                name="perfil"
                value={form.perfil}
                onChange={alterarCampo}
                required
              >
                <option value="Operador">Operador</option>
                <option value="Consulta">Consulta</option>
                {master && <option value="Administrador">Administrador</option>}
              </select>
            </div>
          </div>

          <div className="usuario-modal-footer">
            <button
              type="button"
              className="usuario-btn-cancelar"
              onClick={onFechar}
              disabled={salvando}
            >
              Cancelar
            </button>
            <button
              type="submit"
              className="usuario-btn-salvar"
              disabled={salvando}
            >
              <i className="bi bi-check-circle" />
              {salvando
                ? "Salvando..."
                : editando
                  ? "Salvar alterações"
                  : "Cadastrar usuário"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
