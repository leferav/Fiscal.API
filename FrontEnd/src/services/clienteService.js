import { apiRequestAutenticado } from "./apiClient";

async function requisitarCliente(caminho, opcoes, mensagem) {
  const { response, dados } = await apiRequestAutenticado(caminho, opcoes);
  if (!response.ok) {
    throw new Error(dados?.mensagem || dados?.erro || (typeof dados === "string" ? dados : null) || mensagem);
  }
  return dados;
}

export const obterClientes = () => requisitarCliente("/api/clientes", undefined, "Não foi possível carregar os clientes.");
export const obterClientePorId = (id) => requisitarCliente(`/api/clientes/${id}`, undefined, "Não foi possível consultar o cliente.");
export const cadastrarCliente = (cliente) => requisitarCliente("/api/clientes", { method: "POST", body: JSON.stringify(cliente) }, "Não foi possível cadastrar o cliente.");
export const alterarCliente = (id, cliente) => requisitarCliente(`/api/clientes/${id}`, { method: "PUT", body: JSON.stringify(cliente) }, "Não foi possível atualizar o cliente.");

export const excluirCliente = (id) =>
  requisitarCliente(
    `/api/clientes/${id}`,
    { method: "DELETE" },
    "Não foi possível excluir o cliente."
  );

export const alterarStatusCliente = (id, ativo) =>
  requisitarCliente(
    `/api/clientes/${id}/status`,
    {
      method: "PATCH",
      body: JSON.stringify({ ativo })
    },
    "Não foi possível alterar a situação do cliente."
  );
