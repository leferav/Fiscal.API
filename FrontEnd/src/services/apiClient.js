const API_URL = "https://localhost:7211";

export async function apiRequest(
  endpoint,
  options = {}
) {
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