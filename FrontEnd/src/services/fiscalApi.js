const API_URL = "https://localhost:7211";

export async function obterProdutosPorEmpresa(empresaId) {
  const response = await fetch(
    `${API_URL}/api/cadastros/produtos/empresa/${empresaId}`
  );

  if (!response.ok) {
    throw new Error(
      `Erro ao consultar produtos. HTTP ${response.status}`
    );
  }

  return await response.json();
}

export async function autorizarNFCe(dados) {
  const response = await fetch(
    `${API_URL}/api/nfce/autorizar`,
    {
      method: "POST",

      headers: {
        "Content-Type": "application/json",
      },

      body: JSON.stringify(dados),
    }
  );

  const texto = await response.text();

  let resultado;

  try {
    resultado = texto ? JSON.parse(texto) : {};
  } catch {
    resultado = {
      mensagem: texto,
    };
  }

  if (!response.ok) {
    throw new Error(
      resultado?.mensagem ||
        resultado?.message ||
        `Erro ao emitir NFC-e. HTTP ${response.status}`
    );
  }

  return resultado;
}