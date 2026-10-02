import { obterToken, logout,} from "./authService";

const API_URL = import.meta.env.VITE_API_URL || "https://localhost:7211";
  
export async function apiRequest(endpoint,options = {}) {
  let response;

  try {
    response = await fetch(
      `${API_URL}${endpoint}`,
      options
    );
  } catch (error) {
    console.error(
      "Erro de comunicação com a Fiscal.API:",
      error
    );

    throw new Error(
      "Não foi possível conectar ao servidor Fiscal.API. Verifique sua conexão ou tente novamente em alguns instantes."
    );
  }

  let dados = null;

  try {
    dados = await response.json();
  } catch {
    dados = null;
  }

  return {
    response,
    dados,
  };
}


export async function apiRequestAutenticado(
  endpoint,
  options = {}
) {
  const token = obterToken();

  const headers = {
    ...(options.body
      ? { "Content-Type": "application/json" }
      : {}),
    ...(token
      ? { Authorization: `Bearer ${token}` }
      : {}),
    ...(options.headers || {}),
  };

  const resultado = await apiRequest(
    endpoint,
    {
      ...options,
      headers,
    }
  );

  if (resultado.response.status === 401) {
    logout();

    throw new Error(
      "Sua sessão expirou. Faça login novamente."
    );
  }

  return resultado;
}


export async function apiDownloadAutenticado(
  endpoint,
  options = {}
) {
  const token = obterToken();

  let response;

  try {
    response = await fetch(
      `${API_URL}${endpoint}`,
      {
        ...options,
        headers: {
          ...(token
            ? {
                Authorization: `Bearer ${token}`,
              }
            : {}),
          ...(options.headers || {}),
        },
      }
    );
  } catch (error) {
    console.error(
      "Erro de comunicação com a Fiscal.API:",
      error
    );

    throw new Error(
      "Não foi possível conectar ao servidor Fiscal.API."
    );
  }

  if (response.status === 401) {
    logout();

    throw new Error(
      "Sua sessão expirou. Faça login novamente."
    );
  }

  return response;
}