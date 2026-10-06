import { apiRequestAutenticado } from "./apiClient";

export async function consultarCep(cep) {
  const cepNumerico = String(cep || "")
    .replace(/\D/g, "");

  if (cepNumerico.length !== 8) {
    throw new Error("Informe um CEP válido.");
  }

  const { response, dados } =
    await apiRequestAutenticado(
      `/api/consultas/cep/${cepNumerico}`
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        "Não foi possível consultar o CEP."
    );
  }

  return dados;
}