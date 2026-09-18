import { useState } from "react";

import Login from "./pages/Login/Login";
import EmitirNFCe from "./pages/EmitirNFCe/EmitirNFCe";

import {
  estaAutenticado,
  logout,
} from "./services/authService";

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

  if (!autenticado) {
    return (
      <Login
        onLogin={handleLogin}
      />
    );
  }

  return (
    <EmitirNFCe
      onLogout={handleLogout}
    />
  );
}

export default App;