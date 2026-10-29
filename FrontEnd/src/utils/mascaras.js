import { somenteNumeros } from "./validacoes";

export function mascaraCPF(valor) {
    return somenteNumeros(valor)
        .slice(0, 11)
        .replace(/^(\d{3})(\d)/, "$1.$2")
        .replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3")
        .replace(/\.(\d{3})(\d)/, ".$1-$2");
}

export function mascaraCNPJ(valor) {
    return somenteNumeros(valor)
        .slice(0, 14)
        .replace(/^(\d{2})(\d)/, "$1.$2")
        .replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3")
        .replace(/\.(\d{3})(\d)/, ".$1/$2")
        .replace(/(\d{4})(\d)/, "$1-$2");
}

export function mascaraCEP(valor) {
    return somenteNumeros(valor)
        .slice(0, 8)
        .replace(/^(\d{5})(\d)/, "$1-$2");
}

export function mascaraTelefone(valor) {
    const numeros = somenteNumeros(valor).slice(0, 11);

    if (numeros.length <= 2)
        return numeros ? `(${numeros}` : "";

    if (numeros.length <= 6)
        return `(${numeros.slice(0, 2)}) ${numeros.slice(2)}`;

    if (numeros.length <= 10)
        return `(${numeros.slice(0, 2)}) ${numeros.slice(2, -4)}-${numeros.slice(-4)}`;

    return `(${numeros.slice(0, 2)}) ${numeros.slice(2, 7)}-${numeros.slice(7)}`;
}