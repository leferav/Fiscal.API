import { apiRequestAutenticado, apiDownloadAutenticado } from "./apiClient";

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

export async function obterProdutoPorId(id) {
  const { response, dados } =
    await apiRequestAutenticado(
      `/api/cadastros/produtos/${id}`
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      "Não foi possível carregar o produto."
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

export async function alterarProduto(id, produto) {
  const { response, dados } =
    await apiRequestAutenticado(
      `/api/cadastros/produtos/${id}`,
      {
        method: "PUT",
        body: JSON.stringify(produto),
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      (typeof dados === "string" ? dados : null) ||
      "Não foi possível alterar o produto."
    );
  }

  return dados;
}

export async function pesquisarNcms(busca) {
  const { response, dados } =
    await apiRequestAutenticado(
      `/api/cadastros/ncms?busca=${encodeURIComponent(busca)}`
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      "Não foi possível pesquisar os NCMs."
    );
  }

  return dados;
}


export async function obterImagemProduto(id) {
  const response = await apiDownloadAutenticado(
    `/api/cadastros/produtos/${id}/imagem`
  );

  // Produto sem imagem cadastrada.
  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(
      "Não foi possível carregar a imagem do produto."
    );
  }

  const blob = await response.blob();

  return URL.createObjectURL(blob);
}



export async function enviarImagemProduto(id, arquivo) {
  const formData = new FormData();
  formData.append("arquivo", arquivo);

  const { response, dados } = await apiRequestAutenticado(
    `/api/cadastros/produtos/${id}/imagem`,
    {
      method: "POST",
      body: formData
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      "Não foi possível salvar a imagem do produto."
    );
  }

  return dados;
}

