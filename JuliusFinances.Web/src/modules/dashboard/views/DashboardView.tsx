import { useState, useEffect } from 'react';
import Layout from '@/shared/components/Layout';
import { 
  TrendingUp, 
  TrendingDown, 
  DollarSign, 
  ArrowRight,
  PlusCircle,
  Wallet,
  Calendar
} from 'lucide-react';
import { Link } from 'react-router-dom';

export default function DashboardView() {
  const [userName, setUserName] = useState('Usuário');
  
  // Dados de mock realistas para o dashboard inicial
  const summary = {
    balance: 5240.50,
    incomes: 8500.00,
    expenses: 3259.50,
  };

  const recentTransactions = [
    { id: '1', description: 'Salário Mensal', amount: 8500.00, type: 'income', category: 'Salário', date: '2026-07-05' },
    { id: '2', description: 'Aluguel do Apartamento', amount: 2200.00, type: 'expense', category: 'Habitação', date: '2026-07-06' },
    { id: '3', description: 'Supermercado Imperial', amount: 659.50, type: 'expense', category: 'Alimentação', date: '2026-07-07' },
    { id: '4', description: 'Assinatura de Streaming', amount: 55.00, type: 'expense', category: 'Entretenimento', date: '2026-07-07' },
    { id: '5', description: 'Combustível Posto Ipiranga', amount: 345.00, type: 'expense', category: 'Transporte', date: '2026-07-07' },
  ];

  useEffect(() => {
    try {
      const storedUser = localStorage.getItem('user');
      if (storedUser) {
        const parsed = JSON.parse(storedUser);
        if (parsed && parsed.name) {
          setUserName(parsed.name);
        }
      }
    } catch (e) {
      console.error(e);
    }
  }, []);

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const formatDate = (dateStr: string) => {
    const [year, month, day] = dateStr.split('-');
    return `${day}/${month}/${year}`;
  };

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
          <span className="text-sm font-semibold text-slate-700 dark:text-slate-200">Julho de 2026</span>
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
            <span className="text-xs text-emerald-600 dark:text-emerald-400 font-semibold flex items-center gap-1">
              Saúde financeira positiva
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
            <span className="text-xs text-slate-500 dark:text-slate-400 font-medium">Previsão totalmente quitada</span>
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
            <span className="text-xs text-rose-600 dark:text-rose-400 font-semibold">
              Comprometeu 38,3% da receita
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

          <div className="divide-y divide-slate-100 dark:divide-slate-700/50">
            {recentTransactions.map((transaction) => (
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
                      <span className="text-xs text-slate-400 dark:text-slate-500 font-medium">{transaction.category}</span>
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
            <p className="text-xs text-slate-500 dark:text-slate-400 font-medium mb-4">Seu progresso total de gastos de Julho</p>
            <div className="space-y-4">
              <div>
                <div className="flex justify-between text-xs font-bold text-slate-600 dark:text-slate-300 mb-1.5">
                  <span>Alimentação</span>
                  <span>{formatCurrency(659.50)} / {formatCurrency(1200.00)}</span>
                </div>
                <div className="w-full h-2 bg-slate-100 dark:bg-slate-700 rounded-full overflow-hidden">
                  <div className="h-full bg-indigo-500 rounded-full" style={{ width: '54.9%' }} />
                </div>
              </div>
              
              <div>
                <div className="flex justify-between text-xs font-bold text-slate-600 dark:text-slate-300 mb-1.5">
                  <span>Habitação</span>
                  <span>{formatCurrency(2200.00)} / {formatCurrency(2500.00)}</span>
                </div>
                <div className="w-full h-2 bg-slate-100 dark:bg-slate-700 rounded-full overflow-hidden">
                  <div className="h-full bg-amber-500 rounded-full" style={{ width: '88%' }} />
                </div>
              </div>

              <div>
                <div className="flex justify-between text-xs font-bold text-slate-600 dark:text-slate-300 mb-1.5">
                  <span>Entretenimento</span>
                  <span>{formatCurrency(55.00)} / {formatCurrency(300.00)}</span>
                </div>
                <div className="w-full h-2 bg-slate-100 dark:bg-slate-700 rounded-full overflow-hidden">
                  <div className="h-full bg-emerald-500 rounded-full" style={{ width: '18.3%' }} />
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Layout>
  );
}
