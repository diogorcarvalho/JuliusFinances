import { useState, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider } from '@/shared/hooks/useTheme';
import { ConfirmProvider } from '@/shared/context/ConfirmContext';
import PrivateRoute from '@/core/guards/PrivateRoute';
import PublicRoute from '@/core/guards/PublicRoute';
import LoginView from '@/modules/auth/views/LoginView';
import RegisterView from '@/modules/auth/views/RegisterView';
import DashboardView from '@/modules/dashboard/views/DashboardView';
import TransactionsView from '@/modules/transactions/views/TransactionsView';
import AccountsView from '@/modules/accounts/views/AccountsView';
import CategoriesView from '@/modules/categories/views/CategoriesView';
import { AlertCircle, X } from 'lucide-react';

interface ToastMessage {
  id: string;
  message: string;
}

export default function App() {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  useEffect(() => {
    const handleApiError = (event: Event) => {
      const customEvent = event as CustomEvent<string>;
      const newMessage = {
        id: Math.random().toString(36).substring(2, 9),
        message: customEvent.detail || 'Ocorreu um erro inesperado.',
      };
      
      setToasts((prev) => [...prev, newMessage]);

      // Remove automaticamente após 6 segundos
      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== newMessage.id));
      }, 6000);
    };

    window.addEventListener('api-error', handleApiError);
    return () => {
      window.removeEventListener('api-error', handleApiError);
    };
  }, []);

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  return (
    <ThemeProvider>
      <ConfirmProvider>
        <BrowserRouter>
          <Routes>
            {/* Rotas Públicas */}
            <Route element={<PublicRoute />}>
              <Route path="/login" element={<LoginView />} />
              <Route path="/register" element={<RegisterView />} />
            </Route>

            {/* Rotas Privadas */}
            <Route element={<PrivateRoute />}>
              <Route path="/" element={<Navigate to="/dashboard" replace />} />
              <Route path="/dashboard" element={<DashboardView />} />
              <Route path="/transactions" element={<TransactionsView />} />
              <Route path="/accounts" element={<AccountsView />} />
              <Route path="/categories" element={<CategoriesView />} />
            </Route>

            {/* Rota Fallback */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>

        {/* Container de Toasts/Notificações Flutuantes */}
        <div className="fixed bottom-4 right-4 z-50 flex flex-col gap-2 max-w-sm w-full px-4 sm:px-0">
          {toasts.map((toast) => (
            <div
              key={toast.id}
              className="flex items-start gap-3 bg-red-50 border border-red-200 dark:bg-red-950/90 dark:border-red-900 text-red-800 dark:text-red-200 p-4 rounded-lg shadow-lg transition-all duration-300 transform translate-y-0"
            >
              <AlertCircle className="w-5 h-5 shrink-0 text-red-600 dark:text-red-400 mt-0.5" />
              <div className="flex-1 text-sm font-medium leading-5">{toast.message}</div>
              <button
                onClick={() => removeToast(toast.id)}
                className="text-red-500 hover:text-red-700 dark:text-red-400 dark:hover:text-red-200 transition-colors p-0.5 rounded-full hover:bg-red-100 dark:hover:bg-red-900/50"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          ))}
        </div>
      </ConfirmProvider>
    </ThemeProvider>
  );
}
