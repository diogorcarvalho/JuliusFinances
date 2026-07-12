import { useState, useEffect } from 'react';
import Layout from '@/shared/components/Layout';
import { 
  TrendingUp, 
  TrendingDown, 
  DollarSign, 
  ArrowRight,
  PlusCircle,
  Wallet,
  Calendar,
  Loader2,
  AlertCircle
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { apiClient } from '@/core/api/client';

interface RecentTransaction {
  id: string;
  description: string;
  amount: number;
  type: string;
  categoryName: string;
  date: string;
}

interface CategoryExpense {
  categoryId: string;
  categoryName: string;
  totalSpent: number;
}

interface DashboardSummary {
  balance: number;
  incomes: number;
  expenses: number;
  recentTransactions: RecentTransaction[];
  categoryExpenses: CategoryExpense[];
}

export default function DashboardView() {
  const [userName, setUserName] = useState('Usuário');
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  const BUDGET_LIMITS: { [key: string]: number } = {
    'alimentacao': 1200.00,
    'habitacao': 2500.00,
    'entretenimento': 300.00,
  };

  const BUDGET_LABELS: { [key: string]: string } = {
    'alimentacao': 'Alimentação',
    'habitacao': 'Habitação',
    'entretenimento': 'Entretenimento',
  };

  useEffect(() => {
    let isMounted = true;

    // 1. Carregar nome do usuário do localStorage
    try {
      const storedUser = localStorage.getItem('user');
      if (storedUser) {
        const parsed = JSON.parse(storedUser);
        if (parsed && parsed.name) {
          setUserName(parsed.name);
        }
      }
    } catch (e) {
      console.error('Falha ao ler usuário do localStorage:', e);
    }

    // 2. Buscar resumo consolidado do Dashboard da API
    const fetchDashboardSummary = async () => {
      try {
        setIsLoading(true);
        setError('');
        const response = await apiClient.get<DashboardSummary>('/dashboard/summary');
        if (isMounted) {
          setSummary(response.data);
        }
      } catch (err: any) {
        console.error('Erro ao buscar dados do dashboard:', err);
        if (isMounted) {
          setError('Não foi possível carregar os dados financeiros do servidor. Por favor, verifique sua conexão.');
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    fetchDashboardSummary();

    return () => {
      isMounted = false;
    };
  }, []);

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatDate = (dateStr: string) => {
    try {
      const date = new Date(dateStr);
      return new Intl.DateTimeFormat('pt-BR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        timeZone: 'UTC'
      }).format(date);
    } catch {
      return dateStr;
    }
  };

  const getFormattedCurrentMonth = () => {
    const date = new Date();
    const formatter = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' });
    const formatted = formatter.format(date);
    return formatted.charAt(0).toUpperCase() + formatted.slice(1);
  };

  const getProgressColor = (percentage: number) => {
    if (percentage >= 90) return 'bg-rose-500';
    if (percentage >= 70) return 'bg-amber-500';
    return 'bg-indigo-500';
  };

  const normalizeString = (str: string) => 
    str.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase().trim();

  const getSpentForCategory = (label: string) => {
    if (!summary) return 0;
    const normalizedLabel = normalizeString(label);
    const match = summary.categoryExpenses.find(
      (ce) => normalizeString(ce.categoryName) === normalizedLabel
    );
    return match ? match.totalSpent : 0;
  };

  const budgets = Object.keys(BUDGET_LIMITS).map(key => {
    const label = BUDGET_LABELS[key];
    const limit = BUDGET_LIMITS[key];
    const spent = getSpentForCategory(label);
    const percentage = limit > 0 ? Math.min((spent / limit) * 100, 100) : 0;
    
    return {
      key,
      label,
      limit,
      spent,
      percentage
    };
  });

  if (isLoading) {
    return (
      <Layout>
        <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
          <Loader2 className="w-12 h-12 text-indigo-600 animate-spin" />
          <p className="text-sm text-slate-500 dark:text-slate-400 font-semibold tracking-wide">
            Carregando resumo financeiro...
          </p>
        </div>
      </Layout>
    );
  }

  if (error || !summary) {
    return (
      <Layout>
        <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4 max-w-md mx-auto text-center">
          <div className="w-16 h-16 bg-rose-50 dark:bg-rose-950/30 rounded-2xl flex items-center justify-center text-rose-600 dark:text-rose-400 shadow-md">
            <AlertCircle className="w-8 h-8" />
          </div>
          <h3 className="text-lg font-bold text-slate-900 dark:text-white mt-2">Falha na Conexão</h3>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium leading-relaxed">
            {error || 'Ocorreu um erro desconhecido ao processar os dados.'}
          </p>
          <button 
            onClick={() => window.location.reload()}
            className="mt-2 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-xl text-sm transition-all shadow-md shadow-indigo-600/10"
          >
            Tentar Novamente
          </button>
        </div>
      </Layout>
    );
  }

  return (
    <Layout>
      {/* Cabeçalho */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
            Olá, {userName}!
          </h2>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium mt-1">
            Seja bem-vindo de volta ao seu controle financeiro pessoal.
          </p>
        </div>
        <div className="flex items-center gap-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 px-4 py-2.5 rounded-xl shadow-sm self-start">
          <Calendar className="w-5 h-5 text-indigo-500" />
          <span className="text-sm font-semibold text-slate-700 dark:text-slate-200">{getFormattedCurrentMonth()}</span>
        </div>
      </div>

      {/* Grids de Resumo */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        {/* Card Saldo Geral */}
        <div className="bg-white dark:bg-slate-800 p-6 rounded-2xl border border-slate-200 dark:border-slate-700/50 shadow-sm flex items-center justify-between relative overflow-hidden group">
          <div className="absolute right-0 top-0 w-24 h-24 bg-indigo-500/5 rounded-full translate-x-6 -translate-y-6 group-hover:scale-110 transition-transform duration-300" />
          <div className="space-y-2">
            <span className="text-xs font-bold text-slate-400 dark:text-slate-500 uppercase tracking-wider">Saldo Geral</span>
            <p className="text-2xl md:text-3xl font-extrabold text-slate-950 dark:text-white leading-none">
              {formatCurrency(summary.balance)}
            </p>
            <span className={`text-xs font-semibold flex items-center gap-1 ${
              summary.balance >= 0 ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'
            }`}>
              {summary.balance >= 0 ? 'Saúde financeira positiva' : 'Atenção ao saldo negativo'}
            </span>
          </div>
          <div className="w-12 h-12 rounded-xl bg-indigo-50 dark:bg-indigo-950/50 text-indigo-600 dark:text-indigo-400 flex items-center justify-center shadow-inner">
            <DollarSign className="w-6 h-6" />
          </div>
        </div>

        {/* Card Entradas */}
        <div className="bg-white dark:bg-slate-800 p-6 rounded-2xl border border-slate-200 dark:border-slate-700/50 shadow-sm flex items-center justify-between relative overflow-hidden group">
          <div className="absolute right-0 top-0 w-24 h-24 bg-emerald-500/5 rounded-full translate-x-6 -translate-y-6 group-hover:scale-110 transition-transform duration-300" />
          <div className="space-y-2">
            <span className="text-xs font-bold text-slate-400 dark:text-slate-500 uppercase tracking-wider">Receitas (Mês)</span>
            <p className="text-2xl md:text-3xl font-extrabold text-emerald-600 dark:text-emerald-400 leading-none">
              {formatCurrency(summary.incomes)}
            </p>
            <span className="text-xs text-slate-500 dark:text-slate-400 font-medium">Entradas consolidadas</span>
          </div>
          <div className="w-12 h-12 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shadow-inner">
            <TrendingUp className="w-6 h-6" />
          </div>
        </div>

        {/* Card Saídas */}
        <div className="bg-white dark:bg-slate-800 p-6 rounded-2xl border border-slate-200 dark:border-slate-700/50 shadow-sm flex items-center justify-between relative overflow-hidden group">
          <div className="absolute right-0 top-0 w-24 h-24 bg-rose-500/5 rounded-full translate-x-6 -translate-y-6 group-hover:scale-110 transition-transform duration-300" />
          <div className="space-y-2">
            <span className="text-xs font-bold text-slate-400 dark:text-slate-500 uppercase tracking-wider">Despesas (Mês)</span>
            <p className="text-2xl md:text-3xl font-extrabold text-rose-600 dark:text-rose-400 leading-none">
              {formatCurrency(summary.expenses)}
            </p>
            <span className="text-xs text-slate-500 dark:text-slate-400 font-medium">
              {summary.incomes > 0 
                ? `Comprometeu ${((summary.expenses / summary.incomes) * 100).toFixed(1)}% das receitas` 
                : 'Nenhuma receita registrada'}
            </span>
          </div>
          <div className="w-12 h-12 rounded-xl bg-rose-50 dark:bg-rose-950/50 text-rose-600 dark:text-rose-400 flex items-center justify-center shadow-inner">
            <TrendingDown className="w-6 h-6" />
          </div>
        </div>
      </div>

      {/* Grid Secundário: Transações e Atalhos */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Lista de Transações Recentes */}
        <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 p-6 shadow-sm lg:col-span-2">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h3 className="font-bold text-lg text-slate-900 dark:text-white leading-tight">Transações Recentes</h3>
              <p className="text-xs text-slate-500 dark:text-slate-400 font-medium mt-0.5">Suas últimas movimentações registradas</p>
            </div>
            <Link 
              to="/transactions" 
              className="text-xs font-bold text-indigo-600 hover:text-indigo-700 dark:text-indigo-400 dark:hover:text-indigo-300 flex items-center gap-1.5 transition-colors bg-indigo-50 dark:bg-indigo-950/30 px-3 py-2 rounded-xl"
            >
              <span>Ver Todas</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>

          {summary.recentTransactions.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <p className="text-sm text-slate-400 dark:text-slate-500 font-medium">Nenhuma transação recente encontrada.</p>
              <Link 
                to="/transactions" 
                className="text-xs text-indigo-500 hover:text-indigo-600 font-bold mt-2 hover:underline"
              >
                Adicione sua primeira transação
              </Link>
            </div>
          ) : (
            <div className="divide-y divide-slate-100 dark:divide-slate-700/50">
              {summary.recentTransactions.map((transaction) => (
                <div key={transaction.id} className="flex items-center justify-between py-4 first:pt-0 last:pb-0">
                  <div className="flex items-center gap-3.5 min-w-0">
                    <div className={`w-10 h-10 rounded-xl flex items-center justify-center shrink-0 ${
                      transaction.type === 'income' 
                        ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400' 
                        : 'bg-rose-50 dark:bg-rose-950/40 text-rose-600 dark:text-rose-400'
                    }`}>
                      {transaction.type === 'income' ? <TrendingUp className="w-5 h-5" /> : <TrendingDown className="w-5 h-5" />}
                    </div>
                    <div className="min-w-0">
                      <p className="text-sm font-bold text-slate-900 dark:text-white truncate">{transaction.description}</p>
                      <div className="flex items-center gap-2 mt-0.5">
                        <span className="text-xs text-slate-400 dark:text-slate-500 font-medium">{transaction.categoryName}</span>
                        <span className="w-1 h-1 rounded-full bg-slate-300 dark:bg-slate-600" />
                        <span className="text-xs text-slate-400 dark:text-slate-500 font-medium">{formatDate(transaction.date)}</span>
                      </div>
                    </div>
                  </div>
                  <div className={`text-sm font-extrabold shrink-0 ${
                    transaction.type === 'income' ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'
                  }`}>
                    {transaction.type === 'income' ? '+' : '-'} {formatCurrency(transaction.amount)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Lado Direito: Atalhos / Saúde de Orçamento */}
        <div className="space-y-6">
          {/* Atalhos Rápidos */}
          <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 p-6 shadow-sm">
            <h3 className="font-bold text-lg text-slate-900 dark:text-white mb-4">Ações Rápidas</h3>
            <div className="grid grid-cols-2 gap-3">
              <Link
                to="/transactions"
                className="flex flex-col items-center justify-center p-4 rounded-xl border border-slate-100 dark:border-slate-700/50 bg-slate-50 dark:bg-slate-900/20 hover:bg-indigo-50 dark:hover:bg-indigo-950/20 hover:border-indigo-200 dark:hover:border-indigo-900 text-center transition-all group"
              >
                <PlusCircle className="w-6 h-6 text-indigo-500 mb-2 group-hover:scale-110 transition-transform" />
                <span className="text-xs font-bold text-slate-700 dark:text-slate-200">Nova Transação</span>
              </Link>
              <Link
                to="/accounts"
                className="flex flex-col items-center justify-center p-4 rounded-xl border border-slate-100 dark:border-slate-700/50 bg-slate-50 dark:bg-slate-900/20 hover:bg-indigo-50 dark:hover:bg-indigo-950/20 hover:border-indigo-200 dark:hover:border-indigo-900 text-center transition-all group"
              >
                <Wallet className="w-6 h-6 text-indigo-500 mb-2 group-hover:scale-110 transition-transform" />
                <span className="text-xs font-bold text-slate-700 dark:text-slate-200">Minhas Contas</span>
              </Link>
            </div>
          </div>

          {/* Progresso de Metas/Orçamentos */}
          <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 p-6 shadow-sm">
            <h3 className="font-bold text-lg text-slate-900 dark:text-white mb-2">Orçamento Limite</h3>
            <p className="text-xs text-slate-500 dark:text-slate-400 font-medium mb-4">Seu progresso total de gastos do mês</p>
            <div className="space-y-4">
              {budgets.map((budget) => (
                <div key={budget.key}>
                  <div className="flex justify-between text-xs font-bold text-slate-600 dark:text-slate-300 mb-1.5">
                    <span>{budget.label}</span>
                    <span>{formatCurrency(budget.spent)} / {formatCurrency(budget.limit)}</span>
                  </div>
                  <div className="w-full h-2 bg-slate-100 dark:bg-slate-700 rounded-full overflow-hidden">
                    <div 
                      className={`h-full ${getProgressColor(budget.percentage)} rounded-full transition-all duration-500`} 
                      style={{ width: `${budget.percentage}%` }} 
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </Layout>
  );
}
