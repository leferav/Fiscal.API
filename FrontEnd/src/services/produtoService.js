import { apiRequestAutenticado } from "./apiClient";

export async function obterProdutos() {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/cadastros/produtos"
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      "Não foi possível carregar os produtos."
    );
  }

  return dados;
}

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

export async function cadastrarProduto(produto) {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/cadastros/produtos",
      {
        method: "POST",
        body: JSON.stringify(produto),
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      (typeof dados === "string" ? dados : null) ||
      "Não foi possível cadastrar o produto."
    );
  }

  return dados;
}