import { useEffect, useState } from "react";

import {
  obterEmpresa,
  obterUsuario,
  obterEmpresaTrabalho,
  selecionarEmpresaTrabalho,
} from "../../services/authService";
import { listarEmpresas } from "../../services/empresaService";

export default function Header({ onLogout }) {
  const [menuUsuarioAberto, setMenuUsuarioAberto] = useState(false);
  const usuario = obterUsuario();
  const master = usuario?.perfil === "Master";

  const [empresaSelecionada, setEmpresaSelecionada] = useState(() =>
    obterEmpresaTrabalho()
  );
  const [empresas, setEmpresas] = useState([]);
  const [erroEmpresas, setErroEmpresas] = useState("");
  const [carregandoEmpresas, setCarregandoEmpresas] = useState(false);

  const empresa = master ? empresaSelecionada : obterEmpresa();

  useEffect(() => {
    if (!master) return;

    let ativo = true;

    async function carregarEmpresas() {
      setCarregandoEmpresas(true);
      setErroEmpresas("");

      try {
        const dados = await listarEmpresas();
        if (!ativo) return;

        const empresasAtivas = Array.isArray(dados)
          ? dados.filter((item) => item.ativo)
          : [];

        setEmpresas(empresasAtivas);

        const selecionada = obterEmpresaTrabalho();
        if (
          selecionada &&
          !empresasAtivas.some((item) => item.id === selecionada.id)
        ) {
          localStorage.removeItem("fiscal_empresa_trabalho");
          setEmpresaSelecionada(null);
          window.dispatchEvent(new Event("fiscal-empresa-alterada"));
        }
      } catch (error) {
        if (ativo) {
          setErroEmpresas(
            error?.message || "Não foi possível carregar as empresas."
          );
        }
      } finally {
        if (ativo) setCarregandoEmpresas(false);
      }
    }

    carregarEmpresas();
    return () => {
      ativo = false;
    };
  }, [master]);

  function alterarEmpresa(event) {
    const selecionada = empresas.find(
      (item) => item.id === event.target.value
    );

    if (!selecionada) return;

    selecionarEmpresaTrabalho(selecionada);
    setEmpresaSelecionada(selecionada);
  }

  return (
    <header className="topo">
      <div className="marca">
        <strong>FISCAL.API</strong>
        <span />
      </div>

      <div className="divisor-topo" />

      <div className="titulo-topo">
        <h1>Sistema Fiscal</h1>
        <p>Gestão e emissão de documentos fiscais</p>
      </div>

      <div className="empresa-topo">
        <div className="header-account">
          <div className="header-company-icon">
            <i className="bi bi-building" />
          </div>

          <div className="header-company">
            <span className="header-company-label">
              {master ? "Empresa de trabalho" : "Empresa"}
            </span>

            {master ? (
              <>
                <select
                  className="header-empresa-select"
                  value={empresaSelecionada?.id ?? ""}
                  onChange={alterarEmpresa}
                  disabled={carregandoEmpresas}
                  aria-label="Selecionar empresa de trabalho"
                >
                  <option value="" disabled>
                    {carregandoEmpresas
                      ? "Carregando empresas..."
                      : "Selecione uma empresa"}
                  </option>
                  {empresas.map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.nomeFantasia || item.razaoSocial || item.cnpj}
                    </option>
                  ))}
                </select>
                {erroEmpresas && (
                  <small className="header-empresa-erro" role="alert">
                    {erroEmpresas}
                  </small>
                )}
              </>
            ) : (
              <strong>
                {empresa?.nomeFantasia || empresa?.razaoSocial || "Empresa"}
              </strong>
            )}
          </div>

          <div className="header-user-wrapper">
            <button
              type="button"
              className="header-user-button"
              onClick={() => setMenuUsuarioAberto((aberto) => !aberto)}
            >
              <div className="header-user-avatar">
                {usuario?.nome?.charAt(0).toUpperCase() || "U"}
              </div>

              <div className="header-user-info">
                <strong>{usuario?.nome || "Usuário"}</strong>
                <span>{usuario?.perfil || ""}</span>
              </div>

              <i
                className={
                  menuUsuarioAberto
                    ? "bi bi-chevron-up header-user-arrow"
                    : "bi bi-chevron-down header-user-arrow"
                }
              />
            </button>

            {menuUsuarioAberto && (
              <div className="header-user-menu">
                <div className="header-user-menu-info">
                  <strong>{usuario?.nome}</strong>
                  <span>{usuario?.email}</span>
                </div>

                <div className="header-user-menu-divider" />

                <button
                  type="button"
                  className="header-logout"
                  onClick={onLogout}
                >
                  <i className="bi bi-box-arrow-right me-2" />
                  Sair do sistema
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
