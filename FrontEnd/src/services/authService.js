const API_URL = "https://localhost:7211";

export async function login(email, senha) {
  const response = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",

    headers: {
      "Content-Type": "application/json",
    },

    body: JSON.stringify({
      email,
      senha,
    }),
  });

  const dados = await response.json();

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        "Não foi possível realizar o login."
    );
  }

  return dados;
}

export function salvarSessao(dados) {
  localStorage.setItem(
    "fiscal_token",
    dados.token
  );

  localStorage.setItem(
    "fiscal_usuario",
    JSON.stringify(dados.usuario)
  );

  localStorage.setItem(
    "fiscal_empresa",
    JSON.stringify(dados.empresa)
  );
}

export function obterToken() {
  return localStorage.getItem("fiscal_token");
}

export function obterUsuario() {
  const dados = localStorage.getItem("fiscal_usuario");

  return dados ? JSON.parse(dados) : null;
}

export function obterEmpresa() {
  const dados = localStorage.getItem("fiscal_empresa");

  return dados ? JSON.parse(dados) : null;
}

export function estaAutenticado() {
  return !!obterToken();
}

export function logout() {
  localStorage.removeItem("fiscal_token");
  localStorage.removeItem("fiscal_usuario");
  localStorage.removeItem("fiscal_empresa");
}