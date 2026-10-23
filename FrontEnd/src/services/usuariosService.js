
import { apiRequestAutenticado } from "./apiClient";

export async function listarUsuarios(empresaId = null) {
  const parametros = empresaId
    ? `?empresaId=${encodeURIComponent(empresaId)}`
    : "";

  const { response, dados } = await apiRequestAutenticado(
    `/api/usuarios${parametros}`,
    { method: "GET" }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.erro ||
      (typeof dados === "string" ? dados : null) ||
      "Não foi possível carregar os usuários."
    );
  }

  if (!Array.isArray(dados)) {
    throw new Error("Resposta inesperada ao listar usuários.");
  }

  return dados;
}


export async function cadastrarUsuario(usuario) {
  const { response, dados } = await apiRequestAutenticado(
    "/api/usuarios",
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(usuario)
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.title ||
      (typeof dados === "string" ? dados : null) ||
      "Não foi possível cadastrar o usuário."
    );
  }

  return dados;
}


export async function atualizarUsuario(id, usuario) {
  const { response, dados } = await apiRequestAutenticado(
    `/api/usuarios/${encodeURIComponent(id)}`,
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(usuario)
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.title ||
      (typeof dados === "string" ? dados : null) ||
      "Não foi possível atualizar o usuário."
    );
  }

  return dados;
}

export async function alterarStatusUsuario(id, ativo, empresaId = null) {
  const payload = { ativo };

  // Somente o Master deve informar empresaId.
  if (empresaId) {
    payload.empresaId = empresaId;
  }

  const { response, dados } = await apiRequestAutenticado(
    `/api/usuarios/${encodeURIComponent(id)}/status`,
    {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(payload)
    }
  );

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
      dados?.title ||
      (typeof dados === "string" ? dados : null) ||
      "Não foi possível alterar o status do usuário."
    );
  }

  return dados;
}


