
import { apiRequestAutenticado } from "./apiClient";

const URL = "/api/cadastros/certificados-digitais/meu";

    function validarResposta({ response, dados }) {
    if (!response.ok) {
        throw new Error(
        dados?.mensagem ||
        dados?.erro ||
        dados?.message ||
        `Erro na operação. HTTP ${response.status}`
        );
    }

    return dados;
    }

    // Consultar certificado da empresa autenticada
    export async function obterMeuCertificado() {
    const resultado = await apiRequestAutenticado(URL, {
        method: "GET",
    });

    return validarResposta(resultado);
    }

    // Cadastrar ou renovar certificado A1
    export async function enviarMeuCertificado(arquivo, senha) {
    const formData = new FormData();

    formData.append("Arquivo", arquivo);
    formData.append("Senha", senha);

    const resultado = await apiRequestAutenticado(URL, {
        method: "POST",
        body: formData,
    });

    return validarResposta(resultado);
    }

    // Validar leitura do certificado armazenado
    export async function testarLeituraCertificado() {
    const resultado = await apiRequestAutenticado(
        `${URL}/testar-leitura`,
        { method: "GET" }
    );

    return validarResposta(resultado);
    }

    export async function removerMeuCertificado() {
        const resultado = await apiRequestAutenticado(
            "/api/cadastros/certificados-digitais/meu",
            { method: "DELETE" }
        );

        return validarResposta(resultado);
    }
