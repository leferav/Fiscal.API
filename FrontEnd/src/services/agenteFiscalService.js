
import { apiRequestAutenticado } from "./apiClient";

export async function obterMeusAgentes() {
  const { response, dados } = await apiRequestAutenticado(
    "/api/agentes/meus",
    {
      method: "GET",
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      "Não foi possível carregar os agentes fiscais."
    );
  }

  return Array.isArray(dados) ? dados : [];
}


export async function gerarCodigoVinculacao() {
  const { response, dados } = await apiRequestAutenticado(
    "/api/agentes/vinculacao",
    {
      method: "POST",
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      "Não foi possível gerar o código de vinculação."
    );
  }

  if (!dados?.codigo) {
    throw new Error(
      "A API não retornou o código de vinculação."
    );
  }

  return dados;
}

