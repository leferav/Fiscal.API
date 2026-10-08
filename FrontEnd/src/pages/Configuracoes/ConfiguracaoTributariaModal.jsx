import { useEffect, useState } from "react";

const formularioInicial = {
  nome: "",
  cfop: "",
  cstIcms: "",
  csosn: "",
  aliquotaIcms: "",
};

export default function ConfiguracaoTributariaModal({
  aberto,
  configuracao,
  salvando,
  onFechar,
  onSalvar,
}) {
    const [form, setForm] = useState(formularioInicial);
    const [erroValidacao, setErroValidacao] = useState("");
    const editando = Boolean(configuracao?.id);

    useEffect(() => {
        if (!aberto) {
        return;
        }

        if (configuracao) {
        setForm({
            nome: configuracao.nome || "",
            cfop: configuracao.cfop || "",
            cstIcms: configuracao.cstIcms || "",
            csosn: configuracao.csosn || "",
            aliquotaIcms:
            configuracao.aliquotaIcms ?? "",
        });
        } else {
        setForm(formularioInicial);
        }
    }, [aberto, configuracao]);

    function alterarCampo(event) {
        const { name, value } = event.target;

        setForm((anterior) => ({
            ...anterior,
            [name]: value,
        }));

        setErroValidacao("");
    }

    function salvar(event) {
        event.preventDefault();

        const nome = form.nome.trim();
        const cfop = form.cfop.trim();
        const cstIcms = form.cstIcms.trim();
        const csosn = form.csosn.trim();

        if (!nome) {
        setErroValidacao("Informe o nome da configuração.");
        return;
        }

        if (!/^\d{4}$/.test(cfop)) {
        setErroValidacao("O CFOP deve possuir exatamente 4 números.");
        return;
        }

        if (cstIcms && !/^\d{2}$/.test(cstIcms)) {
        setErroValidacao("O CST ICMS deve possuir exatamente 2 números.");
        return;
        }

        if (csosn && !/^\d{3}$/.test(csosn)) {
        setErroValidacao("O CSOSN deve possuir exatamente 3 números.");
        return;
        }

        const aliquota =
        form.aliquotaIcms === ""
            ? null
            : Number(
                String(form.aliquotaIcms).replace(",", ".")
            );

        if (
        aliquota !== null &&
        (
            Number.isNaN(aliquota) ||
            aliquota < 0 ||
            aliquota > 100
        )
        ) {
        setErroValidacao(
            "A alíquota ICMS deve estar entre 0 e 100."
        );
        return;
        }

        const request = {
        nome: form.nome.trim(),
        cfop: form.cfop.trim(),
        cstIcms:
            form.cstIcms.trim() || null,
        csosn:
            form.csosn.trim() || null,
        aliquotaIcms:
            form.aliquotaIcms === ""
            ? null
            : Number(
                String(form.aliquotaIcms).replace(",", ".")
                ),
        };

        onSalvar(request);
    }

    if (!aberto) {
        return null;
    }

    return (
        <div
        className="configuracoes-modal-overlay"
        onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
            onFechar();
            }
        }}
        >
        <div className="configuracoes-modal">
            <div className="configuracoes-modal-cabecalho">
            <div>
                <h2>
                {editando
                    ? "Editar configuração tributária"
                    : "Nova configuração tributária"}
                </h2>

                <p>
                Informe os parâmetros tributários utilizados
                nos produtos.
                </p>
            </div>

            <button
                type="button"
                className="configuracoes-modal-fechar"
                onClick={onFechar}
                disabled={salvando}
                title="Fechar"
            >
                <i className="bi bi-x-lg" />
            </button>
            </div>

            <form onSubmit={salvar}>
            {erroValidacao && (
            <div className="configuracoes-modal-erro">
                <i className="bi bi-exclamation-circle" />
                {erroValidacao}
            </div>
            )}
            <div className="configuracoes-modal-corpo">
                <div className="configuracoes-campo configuracoes-modal-campo-completo">
                <label>Nome *</label>

                <input
                    name="nome"
                    value={form.nome}
                    onChange={alterarCampo}
                    placeholder="Ex.: Venda interna - CST 00"
                    required
                    autoFocus
                />
                </div>

                <div className="configuracoes-modal-grid">
                <div className="configuracoes-campo">
                    <label>CFOP *</label>

                    <input
                    name="cfop"
                    value={form.cfop}
                    onChange={alterarCampo}
                    placeholder="Ex.: 5102"
                    maxLength={4}
                    required
                    />
                </div>

                <div className="configuracoes-campo">
                    <label>CST ICMS</label>

                    <input
                    name="cstIcms"
                    value={form.cstIcms}
                    onChange={alterarCampo}
                    placeholder="Ex.: 00"
                    maxLength={3}
                    />
                </div>

                <div className="configuracoes-campo">
                    <label>CSOSN</label>

                    <input
                    name="csosn"
                    value={form.csosn}
                    onChange={alterarCampo}
                    placeholder="Ex.: 102"
                    maxLength={3}
                    />
                </div>

                <div className="configuracoes-campo">
                    <label>Alíquota ICMS (%)</label>

                    <input
                    type="number"
                    name="aliquotaIcms"
                    value={form.aliquotaIcms}
                    onChange={alterarCampo}
                    placeholder="Ex.: 17,00"
                    min="0"
                    max="100"
                    step="0.01"
                    />
                </div>
                </div>
            </div>

            <div className="configuracoes-modal-acoes">
                <button
                type="button"
                className="configuracoes-modal-cancelar"
                onClick={onFechar}
                disabled={salvando}
                >
                Cancelar
                </button>

                <button
                type="submit"
                className="configuracoes-modal-salvar"
                disabled={salvando}
                >
                {salvando ? (
                    <>
                    <i className="bi bi-arrow-repeat" />
                    Salvando...
                    </>
                ) : (
                    <>
                    <i className="bi bi-check-lg" />
                    {editando
                        ? "Salvar alterações"
                        : "Cadastrar"}
                    </>
                )}
                </button>
            </div>
            </form>
        </div>
        </div>
    );
}