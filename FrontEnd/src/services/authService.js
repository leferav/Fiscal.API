import { apiRequest } from "./apiClient";


/* ============================================================
   LOGIN
============================================================ */

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


/* ============================================================
   SALVAR SESSÃO
============================================================ */

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


/* ============================================================
   TOKEN
============================================================ */

export function obterToken() {

  return localStorage.getItem(
    "fiscal_token"
  );

}


/* ============================================================
   USUÁRIO
============================================================ */

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


/* ============================================================
   EMPRESA
============================================================ */

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


/* ============================================================
   ID DA EMPRESA
============================================================ */

export function obterEmpresaId() {

  const empresa =
    obterEmpresa();


  return empresa?.id ?? null;

}


/* ============================================================
   EXPIRAÇÃO
============================================================ */

export function obterExpiracao() {

  return localStorage.getItem(
    "fiscal_expira_em"
  );

}


/* ============================================================
   VERIFICAR SE O TOKEN EXPIROU
============================================================ */

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


/* ============================================================
   ESTÁ AUTENTICADO
============================================================ */

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


/* ============================================================
   LOGOUT
============================================================ */

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

}