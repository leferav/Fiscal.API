import { NavLink } from "react-router-dom";

export default function Sidebar() {
  return (
    <aside className="sidebar">
      <nav className="sidebar-nav">

        <NavLink to="/dashboard" className="sidebar-link">
          <i className="bi bi-grid" />
          <span>Dashboard</span>
        </NavLink>

        <div className="sidebar-grupo">
          <span className="sidebar-titulo">VENDAS</span>

          <NavLink to="/vendas/nfce" className={({ isActive }) => `sidebar-link${isActive ? " active" : ""}`}>
            <i className="bi bi-receipt" />
            <span>Emitir NFC-e</span>
          </NavLink>
        </div>

        <div className="sidebar-grupo">
          <span className="sidebar-titulo">FISCAL</span>

          <NavLink to="/fiscal/notas" className="sidebar-link">
            <i className="bi bi-file-earmark-text" />
            <span>Notas Fiscais</span>
          </NavLink>
        </div>

        <div className="sidebar-grupo">
          <span className="sidebar-titulo">CADASTROS</span>

          <NavLink to="/cadastros/produtos" className="sidebar-link">
            <i className="bi bi-box-seam" />
            <span>Produtos</span>
          </NavLink>

          <NavLink to="/cadastros/clientes" className="sidebar-link">
            <i className="bi bi-people" />
            <span>Clientes</span>
          </NavLink>
        </div>

        <div className="sidebar-grupo">
          <span className="sidebar-titulo">SISTEMA</span>

          <NavLink to="/configuracoes" className="sidebar-link">
            <i className="bi bi-gear" />
            <span>Configurações</span>
          </NavLink>
        </div>

      </nav>
    </aside>
  );
}