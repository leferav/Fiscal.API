import { useState } from "react";
import { BrowserRouter, Navigate, Route, Routes,} from "react-router-dom";
import Login from "./pages/Login/Login";
import Produtos from "./pages/Produtos/Produtos";
import ProdutoDetalhes from "./pages/Produtos/ProdutoDetalhes";
import NovoProduto from "./pages/Produtos/NovoProduto";
import EmitirNFCe from "./pages/EmitirNFCe/EmitirNFCe";
import AppLayout from "./components/layout/AppLayout";
import NotasFiscais from "./pages/NotasFiscais/NotasFiscais";
import NotaFiscalDetalhes from "./pages/NotasFiscais/NotaFiscalDetalhes";
import Vendas from "./pages/Vendas/Vendas";
import Dashboard from "./pages/Dashboard/Dashboard";
import Empresa from "./pages/Empresa/Empresa";
import { estaAutenticado, logout, } from "./services/authService";
import Configuracoes from "./pages/Configuracoes/Configuracoes";

function App() {
  const [autenticado, setAutenticado] =
    useState(estaAutenticado());

  function handleLogin() {
    setAutenticado(true);
  }

  function handleLogout() {
    logout();
    setAutenticado(false);
  }

  return (
    <BrowserRouter>
      <Routes>

        {/* LOGIN */}
        <Route
          path="/login"
          element={
            autenticado ? (
              <Navigate to="/vendas/nfce" replace />
            ) : (
              <Login onLogin={handleLogin} />
            )
          }
        />

        {/* ÁREA AUTENTICADA */}
        {autenticado && (
          <Route element={<AppLayout onLogout={handleLogout} />}>

            <Route path="/dashboard" element={<Dashboard />} />

            <Route path="/vendas" element={<Vendas />}  />  

            <Route path="/vendas/nfce" element={<EmitirNFCe onLogout={handleLogout} />}/>

            <Route path="/fiscal/notas" element={<NotasFiscais />} />

            <Route path="/fiscal/notas/:id" element={<NotaFiscalDetalhes />} />

            <Route path="/cadastros/produtos" element={<Produtos />} />

            <Route path="/cadastros/produtos/novo" element={<NovoProduto />} />

            <Route path="/cadastros/produtos/:id" element={<ProdutoDetalhes />} />

            <Route path="/cadastros/produtos/:id/editar" element={<NovoProduto />} />

            <Route path="/cadastros/empresa" element={<Empresa />} />

            <Route path="/configuracoes" element={<Configuracoes />} />

          </Route>
        )}

        {/* ROTA PADRÃO */}
        <Route
          path="*"
          element={
            <Navigate
              to={autenticado ? "/vendas/nfce" : "/login"}
              replace
            />
          }
        />

      </Routes>
    </BrowserRouter>
  );
}

export default App;