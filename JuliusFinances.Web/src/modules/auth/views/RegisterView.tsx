import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { apiClient } from '@/core/api/client';
import { useTheme } from '@/shared/hooks/useTheme';
import { Sun, Moon, Lock, Mail, User, AlertCircle, Loader2, CheckCircle2 } from 'lucide-react';

export default function RegisterView() {
  const { theme, toggleTheme } = useTheme();
  const navigate = useNavigate();
  
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const validatePassword = (pwd: string) => {
    if (pwd.length < 8) return 'A senha deve conter no mínimo 8 caracteres.';
    if (!/[A-Z]/.test(pwd)) return 'A senha deve conter pelo menos uma letra maiúscula.';
    if (!/[a-z]/.test(pwd)) return 'A senha deve conter pelo menos uma letra minúscula.';
    if (!/[0-9]/.test(pwd)) return 'A senha deve conter pelo menos um número.';
    if (!/[!@#$%^&*(),.?":{}|<>]/.test(pwd)) return 'A senha deve conter pelo menos um caractere especial.';
    return '';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!name || !email || !password) {
      setError('Por favor, preencha todos os campos.');
      return;
    }

    if (name.length < 3) {
      setError('O nome deve conter pelo menos 3 caracteres.');
      return;
    }

    const pwdError = validatePassword(password);
    if (pwdError) {
      setError(pwdError);
      return;
    }

    setLoading(true);

    try {
      await apiClient.post('/auth/register', {
        name,
        email,
        password,
      });

      setSuccess(true);
      
      // Espera 2 segundos e redireciona para login
      setTimeout(() => {
        navigate('/login');
      }, 2500);
    } catch (err: any) {
      console.error(err);
      if (err.response && err.response.data && err.response.data.detail) {
        setError(err.response.data.detail);
      } else if (err.response && err.response.status === 409) {
        setError('Este endereço de e-mail já está cadastrado.');
      } else {
        setError('Não foi possível conectar ao servidor. Verifique sua conexão.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50 dark:bg-slate-900 flex flex-col justify-center py-12 sm:px-6 lg:px-8 transition-colors duration-200 relative">
      {/* Botão de Tema Flutuante no Topo */}
      <div className="absolute top-6 right-6">
        <button
          onClick={toggleTheme}
          className="p-3 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-200 rounded-full shadow-md hover:bg-slate-50 dark:hover:bg-slate-700/80 transition-all duration-150"
          aria-label="Alternar tema"
        >
          {theme === 'dark' ? <Sun className="w-5 h-5 text-amber-500" /> : <Moon className="w-5 h-5 text-indigo-500" />}
        </button>
      </div>

      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <div className="flex justify-center mb-4">
          <div className="w-14 h-14 bg-indigo-600 rounded-2xl flex items-center justify-center shadow-lg shadow-indigo-600/30">
            <span className="text-white font-extrabold text-2xl">J</span>
          </div>
        </div>
        <h2 className="text-center text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
          Crie sua conta
        </h2>
        <p className="mt-2 text-center text-sm text-slate-500 dark:text-slate-400">
          Ou{' '}
          <Link to="/login" className="font-semibold text-indigo-600 hover:text-indigo-500 dark:text-indigo-400 dark:hover:text-indigo-300 transition-colors">
            acesse sua conta existente
          </Link>
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md px-4 sm:px-0">
        <div className="bg-white dark:bg-slate-800 py-8 px-6 shadow-xl rounded-2xl border border-slate-100 dark:border-slate-700/50 sm:px-10">
          
          {/* Sucesso no Cadastro */}
          {success ? (
            <div className="text-center py-6">
              <div className="flex justify-center mb-4">
                <CheckCircle2 className="w-16 h-16 text-emerald-500 animate-bounce" />
              </div>
              <h3 className="text-xl font-bold text-slate-900 dark:text-white">Cadastro concluído!</h3>
              <p className="text-sm text-slate-500 dark:text-slate-400 mt-2">
                Sua conta foi criada com sucesso. Redirecionando para a tela de login...
              </p>
            </div>
          ) : (
            <>
              {/* Mensagem de Erro */}
              {error && (
                <div className="mb-6 bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-900 text-red-800 dark:text-red-200 p-4 rounded-xl flex items-start gap-3 animate-pulse">
                  <AlertCircle className="w-5 h-5 shrink-0 text-red-600 dark:text-red-400 mt-0.5" />
                  <div className="text-sm font-medium">{error}</div>
                </div>
              )}

              <form className="space-y-6" onSubmit={handleSubmit}>
                <div>
                  <label htmlFor="name" className="block text-sm font-semibold text-slate-700 dark:text-slate-200">
                    Seu nome completo
                  </label>
                  <div className="mt-1.5 relative rounded-md shadow-sm">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <User className="h-5 w-5 text-slate-400 dark:text-slate-500" />
                    </div>
                    <input
                      id="name"
                      name="name"
                      type="text"
                      autoComplete="name"
                      required
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      className="block w-full pl-10 pr-3 py-3 border border-slate-300 dark:border-slate-700 rounded-xl bg-white dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all shadow-sm"
                      placeholder="Ex: Julius Rock"
                    />
                  </div>
                </div>

                <div>
                  <label htmlFor="email" className="block text-sm font-semibold text-slate-700 dark:text-slate-200">
                    Endereço de e-mail
                  </label>
                  <div className="mt-1.5 relative rounded-md shadow-sm">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <Mail className="h-5 w-5 text-slate-400 dark:text-slate-500" />
                    </div>
                    <input
                      id="email"
                      name="email"
                      type="email"
                      autoComplete="email"
                      required
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      className="block w-full pl-10 pr-3 py-3 border border-slate-300 dark:border-slate-700 rounded-xl bg-white dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all shadow-sm"
                      placeholder="exemplo@gmail.com"
                    />
                  </div>
                </div>

                <div>
                  <label htmlFor="password" className="block text-sm font-semibold text-slate-700 dark:text-slate-200">
                    Crie uma senha forte
                  </label>
                  <div className="mt-1.5 relative rounded-md shadow-sm">
                    <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                      <Lock className="h-5 w-5 text-slate-400 dark:text-slate-500" />
                    </div>
                    <input
                      id="password"
                      name="password"
                      type="password"
                      autoComplete="new-password"
                      required
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                      className="block w-full pl-10 pr-3 py-3 border border-slate-300 dark:border-slate-700 rounded-xl bg-white dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all shadow-sm"
                      placeholder="Mínimo 8 caracteres"
                    />
                  </div>
                  {/* Requisitos de Senha */}
                  <div className="mt-2 text-[11px] text-slate-500 dark:text-slate-400 leading-relaxed bg-slate-50 dark:bg-slate-900/30 p-2.5 rounded-lg border border-slate-100 dark:border-slate-700/50">
                    <span className="font-semibold block mb-0.5">Requisitos da senha:</span>
                    Mínimo 8 caracteres, pelo menos uma letra maiúscula, uma minúscula, um número e um caractere especial (!@#$%^&*).
                  </div>
                </div>

                <div className="pt-2">
                  <button
                    type="submit"
                    disabled={loading}
                    className="w-full flex justify-center py-3 px-4 border border-transparent rounded-xl shadow-lg shadow-indigo-600/10 hover:shadow-indigo-600/20 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 transition-all disabled:opacity-50 disabled:cursor-not-allowed items-center gap-2 h-11"
                  >
                    {loading ? (
                      <>
                        <Loader2 className="w-5 h-5 animate-spin" />
                        <span>Cadastrando...</span>
                      </>
                    ) : (
                      <span>Criar Conta</span>
                    )}
                  </button>
                </div>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}
