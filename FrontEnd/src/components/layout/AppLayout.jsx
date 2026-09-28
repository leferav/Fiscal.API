import { Outlet } from "react-router-dom";
import Header from "./Header";
import Sidebar from "./Sidebar";
import "./AppLayout.css";

export default function AppLayout({ onLogout }) {
  return (
    <div className="pagina">
      <Header onLogout={onLogout} />

      <div className="app-body">
        <Sidebar />

        <div className="app-content">
          <Outlet />
        </div>
      </div>
    </div>
  );
}