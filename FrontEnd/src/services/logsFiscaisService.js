import { apiRequestAutenticado } from "./apiClient";

async function solicitar(caminho) {
  const { response, dados } = await apiRequestAutenticado(caminho);
  if (!response.ok) throw new Error(dados?.mensagem || dados?.message || `Falha ao consultar logs (${response.status}).`);
  return dados;
}

export async function listarLogsFiscais(filtros = {}) {
  const params = new URLSearchParams();
  for (const [chave, valor] of Object.entries(filtros)) {
    if (valor !== null && valor !== undefined && String(valor).trim() !== "") params.set(chave, String(valor));
  }
  return solicitar(`/api/logs-fiscais?${params.toString()}`);
}

export async function obterLogFiscal(id) {
  return solicitar(`/api/logs-fiscais/${encodeURIComponent(id)}`);
}
