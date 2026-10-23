import { useEffect, useState } from "react";
import { listarEmpresas } from "../../services/empresaService";
import { selecionarEmpresaTrabalho } from "../../services/authService";
import "./SelecionarEmpresaModal.css";

export default function SelecionarEmpresaModal({ onLogout }) {
  const [empresas, setEmpresas] = useState([]);
  const [empresaId, setEmpresaId] = useState("");
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState("");
  const [selecionando, setSelecionando] = useState(false);

  useEffect(() => {
    let ativo = true;
    async function carregar() {
      try {
        const resultado = await listarEmpresas();
        if (!ativo) return;
        setEmpresas(
          Array.isArray(resultado)
            ? resultado.filter((empresa) => empresa.ativo)
            : []
        );
      } catch (e) {
        if (ativo) setErro(e?.message || "Erro ao carregar empresas.");
      } finally {
        if (ativo) setCarregando(false);
      }
    }
    carregar();
    return () => { ativo = false; };
  }, []);

  async function confirmar(event) {
    event.preventDefault();
    const empresa = empresas.find((item) => item.id === empresaId);
    if (!empresa) {
      setErro("Selecione uma empresa para continuar.");
      return;
    }
    try {
      setSelecionando(true);
      setErro("");
      await selecionarEmpresaTrabalho(empresa);
    } catch (e) {
      setErro(e?.message || "Não foi possível selecionar a empresa.");
    } finally {
      setSelecionando(false);
    }
  }

  return (
    <div className="selecao-empresa-overlay" role="presentation">
      <div
        className="selecao-empresa-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="selecao-empresa-titulo"
      >
        <div className="selecao-empresa-icone">
          <i className="bi bi-building" />
        </div>
        <h2 id="selecao-empresa-titulo">Selecionar empresa</h2>
        <p>Escolha a empresa com a qual deseja trabalhar no Fiscal.API.</p>

        <form onSubmit={confirmar}>
          <label htmlFor="empresa-trabalho">Empresa de trabalho</label>
          <select
            id="empresa-trabalho"
            value={empresaId}
            onChange={(event) => {
              setEmpresaId(event.target.value);
              setErro("");
            }}
            disabled={carregando || selecionando}
            required
          >
            <option value="">
              {carregando ? "Carregando empresas..." : "Selecione uma empresa"}
            </option>
            {empresas.map((empresa) => (
              <option key={empresa.id} value={empresa.id}>
                {empresa.nomeFantasia || empresa.razaoSocial || empresa.cnpj}
              </option>
            ))}
          </select>

          {erro && <div className="selecao-empresa-erro" role="alert">{erro}</div>}
          {!carregando && !erro && empresas.length === 0 && (
            <div className="selecao-empresa-erro">
              Nenhuma empresa ativa disponível.
            </div>
          )}

          <button
            className="selecao-empresa-confirmar"
            type="submit"
            disabled={!empresaId || carregando || selecionando}
          >
            {selecionando ? "Acessando empresa..." : "Acessar empresa"}
          </button>
        </form>

        <button
          className="selecao-empresa-sair"
          type="button"
          onClick={onLogout}
        >
          Sair do sistema
        </button>
      </div>
    </div>
  );
}
