import { apiRequestAutenticado } from "./apiClient";

export async function obterMinhaConfiguracaoFiscal() {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/cadastros/configuracoes-fiscais/minha"
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        "Não foi possível carregar a configuração fiscal."
    );
  }

  return dados;
}

export async function atualizarMinhaConfiguracaoFiscal(configuracao) {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/cadastros/configuracoes-fiscais/minha",
      {
        method: "PUT",
        body: JSON.stringify(configuracao),
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        "Não foi possível atualizar a configuração fiscal."
    );
  }

  return dados;
}