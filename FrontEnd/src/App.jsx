import { useEffect, useState } from "react";
import { BrowserRouter, Navigate, Route, Routes,} from "react-router-dom";
import Login from "./pages/Login/Login";
import Produtos from "./pages/Produtos/Produtos";
import Clientes from "./pages/Clientes/Clientes";
import ClienteFormulario from "./pages/Clientes/ClienteFormulario";
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
import LogsFiscais from "./pages/LogsFiscais/LogsFiscais";

function App() {
  const [autenticado, setAutenticado] = useState(estaAutenticado());
  const [empresaAtivaId, setEmpresaAtivaId] = useState( () => obterEmpresaTrabalho()?.id ?? null);
  const perfil = obterUsuario()?.perfil;
  const somenteConsulta = autenticado && perfil === "Consulta";
  const rotaInicial = "/dashboard";
  const acessoOperacional = !somenteConsulta;
  const master = autenticado && perfil === "Master";
  const podeVerLogs = autenticado && (perfil === "Master" || perfil === "Administrador");
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
              <Navigate to={rotaInicial} replace />
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

            <Route path="/vendas" element={acessoOperacional ? <Vendas /> : <Navigate to="/dashboard" replace />}  />  

            <Route path="/vendas/nfce" element={acessoOperacional ? <EmitirNFCe onLogout={handleLogout} /> : <Navigate to="/dashboard" replace />}/>

            <Route path="/fiscal/notas" element={<NotasFiscais />} />

            <Route path="/fiscal/notas/:id" element={<NotaFiscalDetalhes />} />

            <Route path="/fiscal/logs" element={podeVerLogs ? <LogsFiscais /> : <Navigate to="/dashboard" replace />} />

            {/* CLIENTES */}
            <Route path="/cadastros/clientes" element={<Clientes />} />
            <Route path="/cadastros/clientes/novo" element={acessoOperacional ? <ClienteFormulario /> : <Navigate to="/dashboard" replace />} />
            <Route path="/cadastros/clientes/:id" element={<ClienteFormulario />} />

            <Route path="/cadastros/produtos" element={<Produtos />} />

            <Route path="/cadastros/produtos/novo" element={acessoOperacional ? <NovoProduto /> : <Navigate to="/dashboard" replace />} />

            <Route path="/cadastros/produtos/:id" element={<ProdutoDetalhes />} />

            <Route path="/cadastros/produtos/:id/editar" element={acessoOperacional ? <NovoProduto /> : <Navigate to="/dashboard" replace />} />

            <Route path="/cadastros/empresa" element={acessoOperacional ? <Empresa /> : <Navigate to="/dashboard" replace />} />

            <Route path="/cadastros/usuarios" element={ podeGerenciarUsuarios() ? (<Usuarios /> ) : ( <Navigate to="/dashboard" replace />)  } />

            <Route path="/configuracoes" element={acessoOperacional ? <Configuracoes /> : <Navigate to="/dashboard" replace />} />

            <Route path="/configuracoes/certificado-digital" element={acessoOperacional ? <CertificadoDigital /> : <Navigate to="/dashboard" replace />} />

          </Route>
        )}

        {/* ROTA PADRÃO */}
        <Route
          path="*"
          element={
            <Navigate
              to={autenticado ? rotaInicial : "/login"}
              replace
            />
          }
        />

      </Routes>
    </BrowserRouter>
  );
}

export default App;