
import { apiRequest } from "./apiClient";


export async function login(email, senha) {
  const { response, dados } =
    await apiRequest(
      "/api/auth/login",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          email,
          senha,
        }),
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.erro ||
      dados?.mensagem ||
      dados?.message ||
      "Não foi possível realizar o login."
    );
  }

  return dados;
}


export function salvarSessao(dados) {

  if (!dados?.token) {

    throw new Error(
      "Token de autenticação não recebido."
    );

  }

  localStorage.setItem(
    "fiscal_token",
    dados.token
  );

  localStorage.setItem(
    "fiscal_usuario",
    JSON.stringify(
      dados.usuario ?? null
    )
  );

  localStorage.removeItem("fiscal_empresa_trabalho");

  localStorage.setItem(
    "fiscal_empresa",
    JSON.stringify(
      dados.empresa ?? null
    )
  );

  /*
    O backend já devolve ExpiraEm.
    Guardamos também essa informação para
    facilitar o controle da sessão.
  */

  if (dados.expiraEm) {

    localStorage.setItem(
      "fiscal_expira_em",
      dados.expiraEm
    );

  } else {

    localStorage.removeItem(
      "fiscal_expira_em"
    );

  }

}


export function obterToken() {

  return localStorage.getItem(
    "fiscal_token"
  );

}


export function obterUsuario() {

  const dados =
    localStorage.getItem(
      "fiscal_usuario"
    );

  if (!dados) {
    return null;
  }

  try {

    return JSON.parse(dados);

  } catch {

    return null;

  }

}


export function obterEmpresa() {

  const dados =
    localStorage.getItem(
      "fiscal_empresa"
    );

  if (!dados) {
    return null;
  }

  try {

    return JSON.parse(dados);

  } catch {

    return null;

  }

}


export function obterEmpresaId() {
  const empresa = obterEmpresaTrabalho();

  return empresa?.id ?? null;
}


export function obterEmpresaTrabalho() {
  const usuario = obterUsuario();

  if (usuario?.perfil !== "Master") {
    return obterEmpresa();
  }

  try {
    const dados = localStorage.getItem(
      "fiscal_empresa_trabalho"
    );

    return dados ? JSON.parse(dados) : null;
  } catch {
    return null;
  }
}


// ============================================================
// MASTER - SELECIONAR EMPRESA E ATUALIZAR JWT
// ============================================================

export async function selecionarEmpresaTrabalho(empresa) {
  const usuario = obterUsuario();

  if (usuario?.perfil !== "Master") {
    throw new Error(
      "Somente o Master pode selecionar uma empresa."
    );
  }

  if (!empresa?.id) {
    throw new Error("Selecione uma empresa válida.");
  }

  const tokenAtual = obterToken();

  if (!tokenAtual) {
    throw new Error(
      "Sessão não encontrada. Faça login novamente."
    );
  }

  // O backend valida a empresa e gera um novo JWT.
  const { response, dados } = await apiRequest(
    "/api/auth/selecionar-empresa",
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${tokenAtual}`,
      },
      body: JSON.stringify({
        empresaId: empresa.id,
      }),
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      "Não foi possível selecionar a empresa."
    );
  }

  if (
    !dados?.token ||
    !dados?.expiraEm ||
    String(dados?.empresaId).toLowerCase() !==
      String(empresa.id).toLowerCase()
  ) {
    throw new Error(
      "Resposta inválida ao selecionar a empresa."
    );
  }

  // Atualiza somente o token e a empresa de trabalho.
  // Não utilizar salvarSessao(), pois ela reinicia a seleção.

  localStorage.setItem(
    "fiscal_token",
    dados.token
  );

  localStorage.setItem(
    "fiscal_expira_em",
    dados.expiraEm
  );

  localStorage.setItem(
    "fiscal_empresa_trabalho",
    JSON.stringify(empresa)
  );

  window.dispatchEvent(
    new Event("fiscal-empresa-alterada")
  );
}


export function obterExpiracao() {

  return localStorage.getItem(
    "fiscal_expira_em"
  );

}


export function tokenExpirado() {

  const token =
    obterToken();

  if (!token) {
    return true;
  }

  try {

    /*
      JWT possui três partes:

      header.payload.signature

      Aqui lemos apenas o payload para
      verificar o campo "exp".
    */

    const partes =
      token.split(".");

    if (partes.length !== 3) {
      return true;
    }

    const payloadBase64 =
      partes[1]
        .replace(/-/g, "+")
        .replace(/_/g, "/");

    const payloadJson =
      decodeURIComponent(
        atob(payloadBase64)
          .split("")
          .map(
            (caractere) =>
              "%" +
              caractere
                .charCodeAt(0)
                .toString(16)
                .padStart(2, "0")
          )
          .join("")
      );

    const payload =
      JSON.parse(payloadJson);

    if (!payload.exp) {
      return true;
    }

    const agora =
      Math.floor(
        Date.now() / 1000
      );

    return payload.exp <= agora;

  } catch (error) {

    console.error(
      "Erro ao verificar JWT:",
      error
    );

    return true;

  }

}


export function estaAutenticado() {

  const token =
    obterToken();

  if (!token) {
    return false;
  }

  if (tokenExpirado()) {

    logout();

    return false;

  }

  return true;

}


export function logout() {

  localStorage.removeItem(
    "fiscal_token"
  );

  localStorage.removeItem(
    "fiscal_usuario"
  );

  localStorage.removeItem(
    "fiscal_empresa"
  );

  localStorage.removeItem(
    "fiscal_expira_em"
  );

  localStorage.removeItem(
    "fiscal_empresa_trabalho"
  );

  window.dispatchEvent(
    new Event("fiscal-sessao-encerrada")
  );

}


export function podeGerenciarUsuarios() {
  const usuario = obterUsuario();

  if (!usuario) {
    return false;
  }

  return ["Master", "Administrador"].includes(
    usuario.perfil
  );
}
