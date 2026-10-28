import { apiRequestAutenticado, apiDownloadAutenticado, } from "./apiClient";

export async function listarNotasFiscais() {
  const { response, dados } =
    await apiRequestAutenticado(
      "/api/notas-fiscais",
      {
        method: "GET",
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      dados?.message ||
      `Erro ao consultar notas fiscais. HTTP ${response.status}`
    );
  }

  return dados;
}

export async function obterNotaFiscalPorId(id) {
  const { response, dados } =
    await apiRequestAutenticado(
      `/api/notas-fiscais/${id}`,
      {
        method: "GET",
      }
    );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      dados?.message ||
      `Erro ao consultar nota fiscal. HTTP ${response.status}`
    );
  }

  return dados;
}

export async function baixarXmlNotaFiscal(id) {
  const response =
    await apiDownloadAutenticado(
      `/api/notas-fiscais/${id}/xml`,
      {
        method: "GET",
      }
    );

  if (!response.ok) {
    let mensagem =
      `Erro ao obter XML. HTTP ${response.status}`;

    try {
      const dados = await response.json();

      mensagem =
        dados?.mensagem ||
        dados?.erro ||
        dados?.message ||
        mensagem;
    } catch {
      // resposta sem JSON
    }

    throw new Error(mensagem);
  }

  return await response.blob();
}

export async function baixarDanfeNotaFiscal(id) {
  const response = await apiDownloadAutenticado(
    `/api/notas-fiscais/${id}/danfe`
  );

  return response.blob();
}

export async function listarVendasPendentes() {
  const { response, dados } = await apiRequestAutenticado(
    "/api/vendas/pendentes",
    {
      method: "GET",
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      dados?.message ||
      `Erro ao consultar vendas pendentes. HTTP ${response.status}`
    );
  }

  return dados?.vendas ?? [];
}


export async function emitirVendaPendente(id) {
  const { response, dados } = await apiRequestAutenticado(
    `/api/vendas/${encodeURIComponent(id)}/emitir`,
    { method: "POST" }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      dados?.message ||
      `Não foi possível concluir a emissão. HTTP ${response.status}`
    );
  }

  return dados;
}


export async function cancelarNotaFiscal(id, justificativa) {
  const { response, dados } = await apiRequestAutenticado(
    `/api/notas-fiscais/${encodeURIComponent(id)}/cancelar`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ justificativa }),
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem || dados?.erro || dados?.message ||
      `Não foi possível solicitar o cancelamento. HTTP ${response.status}`
    );
  }

  return dados;
}
