import { useState } from "react";

import {
  obterEmpresa,
  obterUsuario,
} from "../../services/authService";

export default function Header({ onLogout }) {
  const [menuUsuarioAberto, setMenuUsuarioAberto] =
    useState(false);

  const usuario = obterUsuario();
  const empresa = obterEmpresa();

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
              Empresa
            </span>

            <strong>
              {empresa?.nomeFantasia ||
                empresa?.razaoSocial ||
                "Empresa"}
            </strong>
          </div>

          <div className="header-user-wrapper">
            <button
              type="button"
              className="header-user-button"
              onClick={() =>
                setMenuUsuarioAberto((aberto) => !aberto)
              }
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