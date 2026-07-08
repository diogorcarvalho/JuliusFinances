import { Navigate, Outlet } from 'react-router-dom';
import { isTokenExpired } from '@/core/api/client';

export default function PrivateRoute() {
  let token: string | null = null;
  
  try {
    token = localStorage.getItem('accessToken');
  } catch (error) {
    console.error('Falha ao ler accessToken do localStorage:', error);
  }

  const isInvalid = !token || isTokenExpired(token);

  if (isInvalid) {
    try {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('user');
    } catch (e) {
      // Ignora erro de remoção
    }
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
