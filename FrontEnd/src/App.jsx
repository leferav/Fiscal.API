import { useEffect, useState } from "react";
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
import Configuracoes from "./pages/Configuracoes/Configuracoes";
import {
  estaAutenticado,
  logout,
  podeGerenciarUsuarios,
  obterUsuario,
  obterEmpresaTrabalho,
} from "./services/authService";
import SelecionarEmpresaModal from "./components/layout/SelecionarEmpresaModal";
import CertificadoDigital from "./pages/CertificadoDigital/CertificadoDigital";
import AceitarConvite from "./pages/AceitarConvite/AceitarConvite";
import Usuarios from "./pages/Usuarios/Usuarios";

function App() {
  const [autenticado, setAutenticado] = useState(estaAutenticado());
  const [empresaAtivaId, setEmpresaAtivaId] = useState( () => obterEmpresaTrabalho()?.id ?? null);
  const master = autenticado && obterUsuario()?.perfil === "Master";
  const precisaSelecionarEmpresa = master && !empresaAtivaId;

  useEffect(() => {
    function sessaoEncerrada() {
      setAutenticado(false);
      setEmpresaAtivaId(null);
    }

    window.addEventListener(
      "fiscal-sessao-encerrada",
      sessaoEncerrada
    );

    return () => {
      window.removeEventListener(
        "fiscal-sessao-encerrada",
        sessaoEncerrada
      );
    };
  }, []);    

  useEffect(() => {
    function atualizarEmpresa() {
      setEmpresaAtivaId(obterEmpresaTrabalho()?.id ?? null);
    }
    window.addEventListener("fiscal-empresa-alterada", atualizarEmpresa);
    return () => window.removeEventListener("fiscal-empresa-alterada", atualizarEmpresa);
  }, []);



  function handleLogin() {
    setEmpresaAtivaId(obterEmpresaTrabalho()?.id ?? null);
    setAutenticado(true);
  }

  function handleLogout() {
    logout();
    setEmpresaAtivaId(null);
    setAutenticado(false);
  }

  return (
    <BrowserRouter>
      {precisaSelecionarEmpresa && (
        <SelecionarEmpresaModal onLogout={handleLogout} />
      )}
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

        {/* ACEITAR CONVITE - ROTA PÚBLICA */}
        <Route path="/aceitar-convite" element={<AceitarConvite />}/>

        {/* ÁREA AUTENTICADA */}
        {autenticado && !precisaSelecionarEmpresa && (
          <Route element={<AppLayout key={empresaAtivaId ?? "padrao"} onLogout={handleLogout} />}>

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

            <Route path="/cadastros/usuarios" element={ podeGerenciarUsuarios() ? (<Usuarios /> ) : ( <Navigate to="/dashboard" replace />)  } />

            <Route path="/configuracoes" element={<Configuracoes />} />

            <Route path="/configuracoes/certificado-digital" element={<CertificadoDigital />} />

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