import axios from 'axios';

// Método defensivo para validar expiração do token JWT
export function isTokenExpired(token: string): boolean {
  if (!token) return true;
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return true; // JWT inválido
    
    const payloadBase64 = parts[1];
    if (!payloadBase64) return true;
    
    // Decodificar Base64URL de forma segura
    const base64 = payloadBase64.replace(/-/g, '+').replace(/_/g, '/');
    const decodedJson = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    
    const payload = JSON.parse(decodedJson);
    if (!payload.exp) return false; // Se não tem expiração, assume válido
    
    // Compara com o tempo atual em segundos
    return payload.exp <= Date.now() / 1000;
  } catch (error) {
    console.error('Falha ao decodificar token JWT corrompido:', error);
    return true; // Se der erro, assume expirado/corrompido
  }
}

// Helper para disparar alertas globais de erro
export function triggerApiErrorEvent(message: string) {
  window.dispatchEvent(new CustomEvent('api-error', { detail: message }));
}

// Resolver dinâmico para a URL da API em desenvolvimento
const getApiBaseUrl = (): string => {
  const envUrl = import.meta.env.VITE_API_URL;
  if (import.meta.env.DEV) {
    const hostname = window.location.hostname;
    // Se acessado por IP ou outro host que não seja local, reconstrói dinamicamente para o mesmo host na porta do backend
    if (hostname !== 'localhost' && hostname !== '127.0.0.1') {
      return `http://${hostname}:5290`;
    }
  }
  return envUrl || 'http://localhost:5290';
};

export const apiClient = axios.create({
  baseURL: getApiBaseUrl(),
  timeout: 15000, // Timeout de 15 segundos estabelecido pela especificação
  headers: {
    'Content-Type': 'application/json',
  },
});

// Interceptor de Requisições
apiClient.interceptors.request.use(
  (config) => {
    try {
      const token = localStorage.getItem('accessToken');
      if (token) {
        if (isTokenExpired(token)) {
          localStorage.removeItem('accessToken');
          localStorage.removeItem('user');
        } else {
          config.headers.Authorization = `Bearer ${token}`;
        }
      }
    } catch (error) {
      // Limpeza segura caso de falha de leitura no localStorage
      localStorage.removeItem('accessToken');
      localStorage.removeItem('user');
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor de Respostas
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // 0. Ignorar requisições canceladas intencionalmente para não disparar falsos alertas de erro de rede
    if (axios.isCancel(error)) {
      return Promise.reject(error);
    }

    const { response, code } = error;

    // 1. Tratamento de Desautenticação (410, ou 401 Unauthorized)
    if (response && response.status === 401) {
      localStorage.removeItem('accessToken');
      localStorage.removeItem('user');
      // Redirecionamento forçado para a página de login com parâmetro explicativo
      window.location.href = '/login?session=expired';
      return Promise.reject(error);
    }

    // 2. Erros de rede física, indisponibilidade ou timeouts do servidor
    const isNetworkError =
      !response ||
      code === 'ECONNABORTED' ||
      response.status === 503 ||
      response.status === 504;

    if (isNetworkError) {
      triggerApiErrorEvent('O servidor está temporariamente inacessível. Por favor, verifique sua conexão.');
    }

    return Promise.reject(error);
  }
);
