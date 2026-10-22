
import { useState } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import "./AceitarConvite.css";

export default function AceitarConvite() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const token = searchParams.get("token");

  const [senha, setSenha] = useState("");
  const [confirmarSenha, setConfirmarSenha] = useState("");
  const [carregando, setCarregando] = useState(false);
  const [mensagem, setMensagem] = useState("");
  const [sucesso, setSucesso] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setMensagem("");

    if (!token) {
      setMensagem("Link de convite inválido.");
      return;
    }

    if (senha.length < 12) {
      setMensagem("A senha deve conter pelo menos 12 caracteres.");
      return;
    }

    if (senha !== confirmarSenha) {
      setMensagem("As senhas não coincidem.");
      return;
    }

    setCarregando(true);

    try {
      const baseUrl = import.meta.env.VITE_API_URL;

      if (!baseUrl) {
        throw new Error("URL da API não configurada.");
      }

      const response = await fetch(
        `${baseUrl.replace(/\/$/, "")}/api/auth/aceitar-convite`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            token,
            senha,
            confirmarSenha
          })
        }
      );

      const resultado = await response.json();

      if (!response.ok) {
        throw new Error(
          resultado.mensagem || "Não foi possível ativar a conta."
        );
      }

      setSucesso(true);
      setMensagem("Conta ativada com sucesso!");
      setSenha("");
      setConfirmarSenha("");

      // Remove o token da barra de endereços.
      window.history.replaceState(
        null,
        "",
        "/aceitar-convite"
      );

    } catch (error) {
      setMensagem(error.message || "Erro ao ativar a conta.");
    } finally {
      setCarregando(false);
    }
  }


    return (
    <div className="ativacao-container">
        <main className="ativacao-card">

        <h2>Ativação de conta - Fiscal.API</h2>

        {sucesso ? (
            <div>
            <p>{mensagem}</p>

            <button
                type="button"
                onClick={() => navigate("/login")}
            >
                Ir para o login
            </button>
            </div>
        ) : !token ? (
            <p>Link de convite inválido ou incompleto.</p>
        ) : (
            <form onSubmit={handleSubmit}>

            <p>Defina sua senha para acessar o sistema.</p>

            <label htmlFor="senha">
                Senha
            </label>

            <input
                id="senha"
                type="password"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                minLength={12}
                autoComplete="new-password"
                placeholder="Digite sua senha"
                required
            />

            <label htmlFor="confirmarSenha">
                Confirmar senha
            </label>

            <input
                id="confirmarSenha"
                type="password"
                value={confirmarSenha}
                onChange={(e) => setConfirmarSenha(e.target.value)}
                autoComplete="new-password"
                placeholder="Confirme sua senha"
                required
            />

            {mensagem && (
                <p role="alert" className="ativacao-mensagem">
                {mensagem}
                </p>
            )}

            <button
            type="submit"
            disabled={carregando}
            onClick={() => {
                console.log("BOTÃO ATIVAR CLICADO");
                console.log("Senha preenchida:", senha.length > 0);
                console.log("Confirmação preenchida:", confirmarSenha.length > 0);
                console.log("Senhas iguais:", senha === confirmarSenha);
                console.log("Tamanho da senha:", senha.length);
            }}
            >
            {carregando ? "Ativando..." : "Ativar minha conta"}
            </button>

            </form>
        )}

        </main>
    </div>
    );

}
