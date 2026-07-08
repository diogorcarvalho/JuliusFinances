import { Navigate, Outlet } from 'react-router-dom';
import { isTokenExpired } from '@/core/api/client';

export default function PublicRoute() {
  let token: string | null = null;
  
  try {
    token = localStorage.getItem('accessToken');
  } catch (error) {
    console.error('Falha ao ler accessToken do localStorage:', error);
  }

  const isValid = token && !isTokenExpired(token);

  if (isValid) {
    return <Navigate to="/dashboard" replace />;
  }

  // Se o token estiver expirado ou corrompido, limpa de forma limpa antes de carregar rota anônima
  try {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('user');
  } catch (e) {
    // Ignora erro de remoção
  }

  return <Outlet />;
}
