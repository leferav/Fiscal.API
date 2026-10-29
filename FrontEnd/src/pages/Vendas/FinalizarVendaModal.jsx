import { useState } from "react";

export default function FinalizarVendaModal({
  aberto,
  quantidadeItens,
  valorTotal,
  emitindo,
  erroEmissao,
  clienteSelecionado,
  pesquisaCliente,
  setPesquisaCliente,
  setClienteSelecionado,
  clientesFiltrados,
  erroClientes,
  onFechar,
  onCancelarVenda,
  onConfirmar,
  onEnviarDepois,
}) {
  const [formaPagamento, setFormaPagamento] = useState("01");
  const [cpf, setCpf] = useState("");
  const [erroCpf, setErroCpf] = useState("");

  if (!aberto) {
    return null;
  }

  /* ============================================================
     FORMATAÇÃO DE VALORES
     ============================================================ */

  function formatarMoeda(valor) {
    return Number(valor || 0).toLocaleString("pt-BR", {
      style: "currency",
      currency: "BRL",
    });
  }

  /* ============================================================
     CPF
     ============================================================ */

  function alterarCpf(valor) {
    let numeros = valor
      .replace(/\D/g, "")
      .substring(0, 11);

    numeros = numeros.replace(
      /(\d{3})(\d)/,
      "$1.$2"
    );

    numeros = numeros.replace(
      /(\d{3})(\d)/,
      "$1.$2"
    );

    numeros = numeros.replace(
      /(\d{3})(\d{1,2})$/,
      "$1-$2"
    );

    setCpf(numeros);
    setErroCpf("");
  }

  function cpfValido(valor) {
    const cpfNumeros = valor.replace(/\D/g, "");

    if (cpfNumeros.length !== 11) {
      return false;
    }

    // Impede CPFs como:
    // 00000000000
    // 11111111111
    // 22222222222
    if (/^(\d)\1{10}$/.test(cpfNumeros)) {
      return false;
    }

    let soma = 0;

    /* Primeiro dígito verificador */

    for (let i = 0; i < 9; i++) {
      soma += Number(cpfNumeros[i]) * (10 - i);
    }

    let digito1 = (soma * 10) % 11;

    if (digito1 === 10) {
      digito1 = 0;
    }

    if (digito1 !== Number(cpfNumeros[9])) {
      return false;
    }

    /* Segundo dígito verificador */

    soma = 0;

    for (let i = 0; i < 10; i++) {
      soma += Number(cpfNumeros[i]) * (11 - i);
    }

    let digito2 = (soma * 10) % 11;

    if (digito2 === 10) {
      digito2 = 0;
    }

    return digito2 === Number(cpfNumeros[10]);
  }

  /* ============================================================
     CONFIRMAR VENDA
     ============================================================ */

  function handleConfirmar() {
    if (emitindo) {
      return;
    }

    const cpfNumeros = clienteSelecionado ? "" : cpf.replace(/\D/g, "");

    if (cpfNumeros && !cpfValido(cpfNumeros)) {
      setErroCpf(
        "O CPF informado é inválido. Verifique os números digitados."
      );

      return;
    }

    setErroCpf("");

    onConfirmar({
      formaPagamento,
      cpf: cpfNumeros || null,
    });
  }

  function handleEnviarDepois() {
    if (emitindo) return;

    const cpfNumeros = clienteSelecionado ? "" : cpf.replace(/\D/g, "");
    if (cpfNumeros && !cpfValido(cpfNumeros)) {
      setErroCpf("O CPF informado é inválido. Verifique os números digitados.");
      return;
    }

    setErroCpf("");
    onEnviarDepois?.({ formaPagamento, cpf: cpfNumeros || null });
  }

  /* ============================================================
     TELA
     ============================================================ */

  return (
    <div className="finalizar-overlay">
      <div className="finalizar-modal">

        {/* ======================================================
            CABEÇALHO
            ====================================================== */}

        <div className="finalizar-cabecalho header-gradient">

          <div>
            <span>VENDA</span>
            <h2>Finalizar venda</h2>
          </div>

          <button
            type="button"
            className="finalizar-fechar"
            onClick={onFechar}
            disabled={emitindo}
            title="Fechar"
          >
            <i className="bi bi-x-lg" />
          </button>

        </div>

        {/* ======================================================
            CONTEÚDO
            ====================================================== */}

        <div className="finalizar-conteudo">

          {/* RESUMO */}

          <div className="finalizar-resumo">

            <div>
              <span>Itens</span>

              <strong>
                {quantidadeItens}
              </strong>
            </div>

            <div>
              <span>Total da venda</span>

              <strong>
                {formatarMoeda(valorTotal)}
              </strong>
            </div>

          </div>

          {/* ====================================================
              FORMA DE PAGAMENTO
              ==================================================== */}

          <div className="finalizar-campo">

            <label htmlFor="formaPagamento">
              Forma de pagamento *
            </label>

            <select
              id="formaPagamento"
              value={formaPagamento}
              disabled={emitindo}
              onChange={(e) =>
                setFormaPagamento(e.target.value)
              }
            >
              <option value="01">
                Dinheiro
              </option>

              <option value="03">
                Cartão de crédito
              </option>

              <option value="04">
                Cartão de débito
              </option>

              <option value="17">
                PIX
              </option>
            </select>

          </div>

          {/* Cliente cadastrado ou CPF manual */}
          <div className="finalizar-campo">
            <label>Cliente cadastrado (opcional)</label>
            {clienteSelecionado ? (
              <div className="vendas-cliente-selecionado">
                <div>
                  <strong>{clienteSelecionado.nomeRazaoSocial}</strong>
                  <span>{clienteSelecionado.cpfCnpj} · {clienteSelecionado.cidade || "Cidade não informada"}{clienteSelecionado.uf ? `/${clienteSelecionado.uf}` : ""}</span>
                  <small>Dados do cadastro utilizados na NFC-e.</small>
                </div>
                <button type="button" disabled={emitindo} onClick={() => { setClienteSelecionado(null); setPesquisaCliente(""); }}>Trocar / remover</button>
              </div>
            ) : (
              <>
                <div className="vendas-cliente-busca">
                  <input type="search" value={pesquisaCliente} disabled={emitindo} onChange={(e) => setPesquisaCliente(e.target.value)} placeholder="Pesquisar por nome, CPF ou CNPJ..." aria-label="Pesquisar cliente cadastrado" />
                  {pesquisaCliente.trim() && (
                    <div className="vendas-cliente-resultados">
                      {clientesFiltrados.map((cliente) => (
                        <button key={cliente.id} type="button" disabled={emitindo} onClick={() => { setClienteSelecionado(cliente); setPesquisaCliente(""); setCpf(""); setErroCpf(""); }}>
                          <strong>{cliente.nomeRazaoSocial}</strong><span>{cliente.cpfCnpj}</span>
                        </button>
                      ))}
                      {clientesFiltrados.length === 0 && <span>Nenhum cliente encontrado.</span>}
                    </div>
                  )}
                  {erroClientes && <small className="vendas-cliente-erro">{erroClientes}</small>}
                </div>
                <label htmlFor="cpfVenda">Ou informar CPF na nota</label>
                <input id="cpfVenda" type="text" value={cpf} disabled={emitindo} onChange={(e) => alterarCpf(e.target.value)} placeholder="000.000.000-00" maxLength={14} />
                {erroCpf ? <span className="finalizar-erro">{erroCpf}</span> : <small>Opcional. Sem cliente cadastrado, o CPF pode ser informado manualmente.</small>}
              </>
            )}
          </div>

        </div>

        {/* ======================================================
            AÇÕES
            ====================================================== */}

        <div className="finalizar-acoes">

          {/* ERRO DA EMISSÃO */}

          {erroEmissao && (
            <div className="finalizar-erro-emissao">
              <i className="bi bi-exclamation-triangle" />

              <span>
                {erroEmissao}
              </span>
            </div>
          )}

          {/* CANCELAR VENDA */}

          <button
            type="button"
            className="btn-danger"
            onClick={onCancelarVenda}
            disabled={emitindo}
          >
            <i className="bi bi-x-circle" />
            Cancelar Venda
          </button>

          {/* SALVAR PARA EMISSÃO POSTERIOR */}
          <button
            type="button"
            className="btn-a-enviar"
            style={{ backgroundColor: "#48a9e6", color: "#fff", border: "none", borderRadius: 8, padding: "12px 16px", fontWeight: 600, cursor: emitindo ? "not-allowed" : "pointer" }}
            onClick={handleEnviarDepois}
            disabled={emitindo || !onEnviarDepois}
          >
            {emitindo ? "Processando..." : (
              <>
                <i className="bi bi-clock-history" /> A Enviar
              </>
            )}
          </button>

          {/* EMITIR NFC-e */}

          <button
            type="button"
            className="btn-success"
            onClick={handleConfirmar}
            disabled={emitindo}
          >
            {emitindo ? (
              <>
                <span
                  className="spinner-border spinner-border-sm"
                  aria-hidden="true"
                />

                Emitindo...
              </>
            ) : (
              <>
                <i className="bi bi-check-circle" />

                Confirmar e emitir NFC-e
              </>
            )}
          </button>

        </div>

      </div>
    </div>
  );
}