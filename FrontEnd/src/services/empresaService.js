import { apiRequestAutenticado } from "./apiClient";

export async function obterMinhaEmpresa() {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/cadastros/empresas/minha"
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        "Não foi possível carregar os dados da empresa."
    );
  }

  return dados;
}

export async function atualizarMinhaEmpresa(dadosEmpresa) {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/cadastros/empresas/minha",
      {
        method: "PUT",
        body: JSON.stringify(dadosEmpresa),
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        "Não foi possível atualizar os dados da empresa."
    );
  }

  return dados;
}
// Disponível apenas para o perfil Master.
export async function listarEmpresas() {
  const { response, dados } = await apiRequestAutenticado(
    "/api/cadastros/empresas",
    { method: "GET" }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      (typeof dados === "string" ? dados : null) ||
      "Não foi possível listar as empresas."
    );
  }

  if (!Array.isArray(dados)) {
    throw new Error("Resposta inesperada ao listar empresas.");
  }

  return dados;
}
