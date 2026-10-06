import { NavLink } from "react-router-dom";

export default function Sidebar() {
  return (
    <aside className="sidebar">
      <nav className="sidebar-nav">

        <NavLink
          to="/dashboard"
          className={({ isActive }) =>
            `sidebar-link${isActive ? " active" : ""}`
          }
        >
          <i className="bi bi-grid" />
          <span>Dashboard</span>
        </NavLink>

        <div className="sidebar-grupo">
          <span className="sidebar-titulo">VENDAS</span>

          <NavLink
            to="/vendas"
            end
            className={({ isActive }) =>
              `sidebar-link${isActive ? " active" : ""}`
            }
          >
            <i className="bi bi-cart3" />
            <span>Vendas</span>
          </NavLink>

          <NavLink
            to="/vendas/nfce"
            className={({ isActive }) =>
              `sidebar-link${isActive ? " active" : ""}`
            }
          >
            <i className="bi bi-receipt" />
            <span>Emitir NFC-e</span>
          </NavLink>
        </div>

        <div className="sidebar-grupo">
          <span className="sidebar-titulo">FISCAL</span>

          <NavLink
            to="/fiscal/notas"
            className={({ isActive }) =>
              `sidebar-link${isActive ? " active" : ""}`
            }
          >
            <i className="bi bi-file-earmark-text" />
            <span>Notas Fiscais</span>
          </NavLink>
        </div>

        <div className="sidebar-grupo">
          <span className="sidebar-titulo">CADASTROS</span>

          <NavLink
            to="/cadastros/produtos"
            className={({ isActive }) =>
              `sidebar-link${isActive ? " active" : ""}`
            }
          >
            <i className="bi bi-box-seam" />
            <span>Produtos</span>
          </NavLink>

          <NavLink
            to="/cadastros/empresa"
            className={({ isActive }) =>
              `sidebar-link${isActive ? " active" : ""}`
            }
          >
            <i className="bi bi-people" />
            <span>Empresa</span>
          </NavLink>
        </div>

        <div className="sidebar-grupo">
          <span className="sidebar-titulo">SISTEMA</span>

          <NavLink
            to="/configuracoes"
            className={({ isActive }) =>
              `sidebar-link${isActive ? " active" : ""}`
            }
          >
            <i className="bi bi-gear" />
            <span>Configurações</span>
          </NavLink>
        </div>

      </nav>
    </aside>
  );
}