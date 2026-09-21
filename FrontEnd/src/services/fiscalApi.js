import {
  obterToken,
  logout,
} from "./authService";

import {
  apiRequest,
} from "./apiClient";


/* ============================================================
   HEADERS AUTENTICADOS
============================================================ */

function criarHeaders(
  contentType = true
) {

  const token =
    obterToken();


  const headers = {};


  if (contentType) {

    headers["Content-Type"] =
      "application/json";

  }


  if (token) {

    headers["Authorization"] =
      `Bearer ${token}`;

  }


  return headers;

}


/* ============================================================
   TRATAR RESPOSTA NÃO AUTORIZADA
============================================================ */

function verificarAutenticacao(
  response
) {

  if (response.status === 401) {

    /*
      Token inválido ou expirado.

      Limpamos a sessão para impedir
      que o frontend continue utilizando
      credenciais inválidas.
    */

    logout();


    throw new Error(
      "Sua sessão expirou. Faça login novamente."
    );

  }

}


/* ============================================================
   CONSULTAR PRODUTOS DA EMPRESA
============================================================ */

export async function obterProdutosPorEmpresa(
  empresaId
) {
  if (!empresaId) {
    throw new Error(
      "Empresa não informada para consultar os produtos."
    );
  }

  const { response, dados } =
    await apiRequest(
      `/api/cadastros/produtos/empresa/${empresaId}`,
      {
        method: "GET",
        headers: criarHeaders(false),
      }
    );

  verificarAutenticacao(response);

  if (!response.ok) {
    throw new Error(
      dados?.erro ||
      dados?.mensagem ||
      dados?.message ||
      `Erro ao consultar produtos. HTTP ${response.status}`
    );
  }

  return dados;
}

/* ============================================================
   AUTORIZAR NFC-e
============================================================ */

export async function autorizarNFCe(
  dados
) {
  const {
    response,
    dados: resultado,
  } = await apiRequest(
    "/api/nfce/autorizar",
    {
      method: "POST",
      headers: criarHeaders(true),
      body: JSON.stringify(dados),
    }
  );

  verificarAutenticacao(response);

  if (!response.ok) {
    throw new Error(
      resultado?.erro ||
      resultado?.mensagem ||
      resultado?.message ||
      `Erro ao emitir NFC-e. HTTP ${response.status}`
    );
  }

  return resultado;
}