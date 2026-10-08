import { apiRequestAutenticado } from "./apiClient";

export async function obterConfiguracoesTributarias() {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/cadastros/configuracoes-tributarias"
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        dados?.erro ||
        "Não foi possível carregar as configurações tributárias."
    );
  }

  return dados;
}

export async function cadastrarConfiguracaoTributaria(configuracao) {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/cadastros/configuracoes-tributarias",
      {
        method: "POST",
        body: JSON.stringify(configuracao),
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        dados?.erro ||
        (typeof dados === "string" ? dados : null) ||
        "Não foi possível cadastrar a configuração tributária."
    );
  }

  return dados;
}

export async function atualizarConfiguracaoTributaria(
  id,
  configuracao
) {
  const { response, dados } =
    await apiRequestAutenticado(
      `/api/cadastros/configuracoes-tributarias/${id}`,
      {
        method: "PUT",
        body: JSON.stringify(configuracao),
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        dados?.erro ||
        (typeof dados === "string" ? dados : null) ||
        "Não foi possível atualizar a configuração tributária."
    );
  }

  return dados;
}

export async function desativarConfiguracaoTributaria(id) {
  const { response, dados } =
    await apiRequestAutenticado(
      `/api/cadastros/configuracoes-tributarias/${id}`,
      {
        method: "DELETE",
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        dados?.erro ||
        (typeof dados === "string" ? dados : null) ||
        "Não foi possível desativar a configuração tributária."
    );
  }

  return dados;
}