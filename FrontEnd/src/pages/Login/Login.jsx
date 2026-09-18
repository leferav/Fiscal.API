import { useState } from "react";

import {
  login,
  salvarSessao,
} from "../../services/authService";

import "./Login.css";

function Login({ onLogin }) {
  const [email, setEmail] = useState("");
  const [senha, setSenha] = useState("");
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState("");

  async function entrar(event) {
    event.preventDefault();

    if (!email.trim() || !senha) {
      setErro("Informe o e-mail e a senha.");
      return;
    }

    try {
      setCarregando(true);
      setErro("");

      const dados = await login(
        email.trim(),
        senha
      );

      salvarSessao(dados);

      onLogin(dados);
    } catch (error) {
      setErro(
        error.message ||
          "Não foi possível realizar o login."
      );
    } finally {
      setCarregando(false);
    }
  }

  return (
    <main className="login-page">
      <div className="container">
        <div className="row min-vh-100 align-items-center justify-content-center">

          <div className="col-12 col-sm-10 col-md-7 col-lg-5 col-xl-4">

            <div className="login-card">

              {/* Marca */}
              <div className="login-brand">
                <div className="login-logo">
                  FISCAL.API
                </div>

                <div className="login-brand-line" />
              </div>

              {/* Título */}
              <div className="mb-4">
                <h1 className="login-title">
                  Acesso ao sistema
                </h1>

                <p className="login-subtitle">
                  Entre com seus dados para acessar
                  a emissão de NFC-e
                </p>
              </div>

              <form onSubmit={entrar}>

                {/* E-mail */}
                <div className="mb-3">
                  <label
                    htmlFor="email"
                    className="form-label login-label"
                  >
                    E-mail
                  </label>

                  <div className="input-group login-input-group">

                    <span className="input-group-text">
                      <i className="bi bi-envelope"></i>
                    </span>

                    <input
                      id="email"
                      type="email"
                      className="form-control"
                      value={email}
                      onChange={(e) =>
                        setEmail(e.target.value)
                      }
                      placeholder="seu@email.com"
                      autoComplete="username"
                      autoFocus
                    />

                  </div>
                </div>

                {/* Senha */}
                <div className="mb-3">
                  <label
                    htmlFor="senha"
                    className="form-label login-label"
                  >
                    Senha
                  </label>

                  <div className="input-group login-input-group">

                    <span className="input-group-text">
                      <i className="bi bi-lock"></i>
                    </span>

                    <input
                      id="senha"
                      type={
                        mostrarSenha
                          ? "text"
                          : "password"
                      }
                      className="form-control"
                      value={senha}
                      onChange={(e) =>
                        setSenha(e.target.value)
                      }
                      placeholder="Digite sua senha"
                      autoComplete="current-password"
                    />

                    <button
                      type="button"
                      className="btn login-show-password"
                      onClick={() =>
                        setMostrarSenha(
                          (valor) => !valor
                        )
                      }
                      title={
                        mostrarSenha
                          ? "Ocultar senha"
                          : "Mostrar senha"
                      }
                    >
                      <i
                        className={
                          mostrarSenha
                            ? "bi bi-eye-slash"
                            : "bi bi-eye"
                        }
                      ></i>
                    </button>

                  </div>
                </div>

                {/* Erro */}
                {erro && (
                  <div
                    className="alert alert-danger login-alert"
                    role="alert"
                  >
                    <i className="bi bi-exclamation-circle me-2"></i>

                    {erro}
                  </div>
                )}

                {/* Botão */}
                <div className="d-grid mt-4">

                  <button
                    type="submit"
                    className="btn login-button"
                    disabled={carregando}
                  >
                    {carregando ? (
                      <>
                        <span
                          className="spinner-border spinner-border-sm me-2"
                          aria-hidden="true"
                        ></span>

                        Entrando...
                      </>
                    ) : (
                      <>
                        <i className="bi bi-box-arrow-in-right me-2"></i>
                        Entrar
                      </>
                    )}
                  </button>

                </div>

              </form>

              <div className="login-footer">
                <i className="bi bi-shield-check me-1"></i>
                Acesso seguro • FISCAL.API
              </div>

            </div>

          </div>
        </div>
      </div>
    </main>
  );
}

export default Login;