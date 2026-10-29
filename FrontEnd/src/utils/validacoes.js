export const somenteNumeros = (valor) =>
    String(valor ?? "").replace(/\D/g, "");

export function validarCPF(valor) {
    const cpf = somenteNumeros(valor);

    if (!/^\d{11}$/.test(cpf)) return false;
    if (/^(\d)\1{10}$/.test(cpf)) return false;

    for (let tamanho = 9; tamanho <= 10; tamanho++) {
        let soma = 0;

        for (let i = 0; i < tamanho; i++) {
            soma += Number(cpf[i]) * (tamanho + 1 - i);
        }

        let digito = (soma * 10) % 11;
        if (digito === 10) digito = 0;

        if (digito !== Number(cpf[tamanho])) return false;
    }

    return true;
}

export function validarCNPJ(valor) {
    const cnpj = somenteNumeros(valor);

    // Validação do CNPJ numérico tradicional.
    if (!/^\d{14}$/.test(cnpj)) return false;
    if (/^(\d)\1{13}$/.test(cnpj)) return false;

    const pesos1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    const pesos2 = [6, ...pesos1];

    const calcular = (pesos) => {
        const soma = pesos.reduce(
            (total, peso, i) => total + Number(cnpj[i]) * peso,
            0
        );

        const resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    };

    return calcular(pesos1) === Number(cnpj[12]) &&
           calcular(pesos2) === Number(cnpj[13]);
}

export function validarEmail(valor) {
    const email = String(valor ?? "").trim();

    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

export function validarCEP(valor) {
    return /^\d{8}$/.test(somenteNumeros(valor));
}

export function validarTelefone(valor) {
    const telefone = somenteNumeros(valor);
    return telefone.length === 10 || telefone.length === 11;
}