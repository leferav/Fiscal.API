
import { apiRequestAutenticado } from "./apiClient";

export async function obterMeuCertificadoDigital() {
  const { response, dados } = await apiRequestAutenticado(
    "/api/cadastros/certificados-digitais/meu"
  );

  // Certificado ainda não configurado.
  if (response.status === 404) {
    return null;
  }

  if (!response.ok) {
    throw new Error(
      dados?.mensagem ||
        "Não foi possível carregar o certificado digital."
    );
  }

  return dados;
}
