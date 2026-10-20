
import { useEffect, useState } from "react";
import {
  obterMeuCertificado,
  enviarMeuCertificado,
  testarLeituraCertificado,
  removerMeuCertificado,
} from "../../services/certificadoDigitalService";

export default function CertificadoDigital() {
  const [certificado, setCertificado] = useState(null);
  const [arquivo, setArquivo] = useState(null);
  const [senha, setSenha] = useState("");
  const [mostrarSenha, setMostrarSenha] = useState(false);
  const [carregando, setCarregando] = useState(true);
  const [salvando, setSalvando] = useState(false);
  const [testando, setTestando] = useState(false);
  const [mensagem, setMensagem] = useState("");
  const [erro, setErro] = useState("");
  const [removendo, setRemovendo] = useState(false);
  const [confirmarRemocao, setConfirmarRemocao] = useState(false);

  useEffect(() => {
    carregarCertificado();
  }, []);

  async function carregarCertificado() {
    try {
      setCarregando(true);
      setErro("");

      const resultado = await obterMeuCertificado();

      setCertificado(
        resultado.configurado ? resultado.dados : null
      );
    } catch (error) {
      setErro(error.message);
    } finally {
      setCarregando(false);
    }
  }

  async function salvarCertificado(event) {
    event.preventDefault();

    if (!arquivo || !senha.trim()) {
      setErro("Selecione o certificado e informe a senha.");
      return;
    }

    if (arquivo.size > 5 * 1024 * 1024) {
      setErro("O certificado deve ter no máximo 5 MB.");
      return;
    }

    if (!/\.(pfx|p12)$/i.test(arquivo.name)) {
      setErro("Selecione um arquivo .pfx ou .p12.");
      return;
    }

    try {
      setSalvando(true);
      setErro("");
      setMensagem("");

      const resultado = await enviarMeuCertificado(
        arquivo,
        senha
      );

      setMensagem(resultado.mensagem);
      setArquivo(null);
      setSenha("");

      await carregarCertificado();
    } catch (error) {
      setErro(error.message);
    } finally {
      setSalvando(false);
    }
  }

  async function testarCertificado() {
    try {
      setTestando(true);
      setErro("");
      setMensagem("");

      const resultado = await testarLeituraCertificado();

      if (resultado.certificadoValido) {
        setMensagem(
          "Certificado validado com sucesso. Leitura, CNPJ, chave privada e validade conferidos."
        );
      } else {
        setErro(
          "O certificado foi localizado, mas não passou em todas as validações."
        );
      }
    } catch (error) {
      setErro(error.message);
    } finally {
      setTestando(false);
    }
  }

  function formatarData(data) {
    if (!data) return "-";

    return new Date(data).toLocaleDateString("pt-BR", {
      timeZone: "America/Sao_Paulo",
    });
  }

  async function excluirCertificado() {
    try {
        setRemovendo(true);
        setErro("");
        setMensagem("");

        const resultado = await removerMeuCertificado();

        setCertificado(null);
        setArquivo(null);
        setSenha("");
        setConfirmarRemocao(false);

        setMensagem(resultado.mensagem);
    } catch (error) {
        setErro(error.message);
    } finally {
        setRemovendo(false);
    }
  }

  const vencido =
    certificado &&
    new Date(certificado.validoAte).getTime() < Date.now();

  const diasRestantes = certificado
    ? Math.ceil(
        (new Date(certificado.validoAte).getTime() -
          Date.now()) /
          86400000
      )
    : null;

  return (
    <main className="conteudo certificado-pagina">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <span className="label-azul">CONFIGURAÇÕES</span>
          <h2>Certificado Digital</h2>
          <p className="text-muted">
            Gerencie o certificado A1 da sua empresa.
          </p>
        </div>

        <button
          type="button"
          className="btn btn-outline-primary"
          onClick={carregarCertificado}
          disabled={carregando}
        >
          <i className="bi bi-arrow-clockwise me-2" />
          Atualizar
        </button>
      </div>

      {erro && (
        <div className="alert alert-danger">{erro}</div>
      )}

      {mensagem && (
        <div className="alert alert-success">{mensagem}</div>
      )}

      <section className="card p-4 mb-4">
        <h5 className="mb-3">
          <i className="bi bi-shield-check me-2" />
          Certificado atual
        </h5>

        {carregando ? (
          <p>Carregando certificado...</p>
        ) : !certificado ? (
          <div className="alert alert-warning mb-0">
            Nenhum certificado digital cadastrado para esta empresa.
          </div>
        ) : (
          <>
            <div className="mb-3">
              <span
                className={`badge ${
                  !certificado.ativo ||
                  vencido ||
                  !certificado.arquivoConfigurado
                    ? "bg-danger"
                    : diasRestantes <= 30
                      ? "bg-warning text-dark"
                      : "bg-success"
                }`}
              >
                {!certificado.ativo
                  ? "Inativo"
                  : !certificado.arquivoConfigurado
                    ? "Arquivo não configurado"
                    : vencido
                      ? "Vencido"
                      : diasRestantes <= 30
                        ? "Próximo do vencimento"
                        : "Ativo"}
              </span>
            </div>

            <div className="row g-3">
              <Campo titulo="Titular" valor={certificado.titular} />
              <Campo titulo="CNPJ" valor={certificado.cnpj} />
              <Campo titulo="Emissor" valor={certificado.emissor} />
              <Campo
                titulo="Válido de"
                valor={formatarData(certificado.validoDe)}
              />
              <Campo
                titulo="Válido até"
                valor={formatarData(certificado.validoAte)}
              />
              <Campo
                titulo="Arquivo"
                valor={certificado.nomeArquivo}
              />
            </div>

            <button
              type="button"
              className="btn btn-outline-success mt-4"
              onClick={testarCertificado}
              disabled={testando}
            >
              <i className="bi bi-check-circle me-2" />
              {testando
                ? "Validando..."
                : "Testar certificado"}
            </button>
            <button
                type="button"
                className="btn btn-outline-danger mt-4 ms-2"
                onClick={() => setConfirmarRemocao(true)}
                >
                <i className="bi bi-trash me-2" />
                Remover certificado
            </button>
          </>
        )}
      </section>

      <section className="card p-4">
        <h5 className="mb-3">
          <i className="bi bi-file-earmark-lock me-2" />
          {certificado
            ? "Renovar certificado"
            : "Cadastrar certificado"}
        </h5>

        <form onSubmit={salvarCertificado}>
          <div className="mb-3">
            <label className="form-label">
              Arquivo do certificado A1
            </label>

            <input
              type="file"
              className="form-control"
              accept=".pfx,.p12"
              onChange={(e) =>
                setArquivo(e.target.files?.[0] || null)
              }
              required
            />
            <small className="text-muted">
              Formatos aceitos: .pfx ou .p12 — máximo 5 MB.
            </small>
          </div>

          <div className="mb-3">
            <label className="form-label">
              Senha do certificado
            </label>

            <div className="input-group">
              <input
                type={mostrarSenha ? "text" : "password"}
                className="form-control"
                value={senha}
                onChange={(e) => setSenha(e.target.value)}
                autoComplete="new-password"
                required
              />

              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={() => setMostrarSenha(!mostrarSenha)}
                aria-label={
                  mostrarSenha
                    ? "Ocultar senha"
                    : "Mostrar senha"
                }
              >
                <i
                  className={`bi ${
                    mostrarSenha ? "bi-eye-slash" : "bi-eye"
                  }`}
                />
              </button>
            </div>
          </div>

          <button
            type="submit"
            className="btn btn-primary"
            disabled={salvando}
          >
            <i className="bi bi-cloud-upload me-2" />
            {salvando
              ? "Salvando..."
              : "Salvar certificado"}
          </button>
        </form>
      </section>
        {confirmarRemocao && (
        <div
            className="modal d-block"
            tabIndex="-1"
            role="dialog"
            aria-modal="true"
            style={{ backgroundColor: "rgba(0,0,0,0.5)" }}
        >
            <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content">
                <div className="modal-header">
                <h5 className="modal-title">
                    <i className="bi bi-exclamation-triangle text-danger me-2" />
                    Remover certificado digital
                </h5>
                </div>

                <div className="modal-body">
                <p>
                    Deseja realmente remover o certificado digital
                    desta empresa?
                </p>

                <p className="text-danger mb-0">
                    Após a exclusão, não será possível emitir novas
                    NFC-e até cadastrar outro certificado válido.
                </p>
                </div>

                <div className="modal-footer">
                <button
                    type="button"
                    className="btn btn-secondary"
                    onClick={() => setConfirmarRemocao(false)}
                    disabled={removendo}
                >
                    Cancelar
                </button>

                <button
                    type="button"
                    className="btn btn-danger"
                    onClick={excluirCertificado}
                    disabled={removendo}
                >
                    {removendo
                    ? "Removendo..."
                    : "Sim, remover certificado"}
                </button>
                </div>
            </div>
            </div>
        </div>
        )}

    </main>
  );
}

function Campo({ titulo, valor }) {
  return (
    <div className="col-md-6">
      <small className="text-muted d-block">{titulo}</small>
      <strong className="text-break">{valor || "-"}</strong>
    </div>
  );
}
