import { useState } from 'react';
import Layout from '@/shared/components/Layout';
import { useConfirm } from '@/shared/context/ConfirmContext';
import { 
  TrendingUp, 
  TrendingDown, 
  Search, 
  Plus, 
  Trash2, 
  X 
} from 'lucide-react';

interface Transaction {
  id: string;
  description: string;
  amount: number;
  type: 'income' | 'expense';
  category: string;
  date: string;
}

export default function TransactionsView() {
  const confirm = useConfirm();
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<'all' | 'income' | 'expense'>('all');
  const [isModalOpen, setIsModalOpen] = useState(false);

  // Campos do formulário de criação de transações
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [type, setType] = useState<'income' | 'expense'>('expense');
  const [category, setCategory] = useState('Alimentação');
  const [date, setDate] = useState(new Date().toISOString().split('T')[0]);

  // Transações iniciais com controle de estado local interativo
  const [transactions, setTransactions] = useState<Transaction[]>([
    { id: '1', description: 'Salário Mensal', amount: 8500.00, type: 'income', category: 'Salário', date: '2026-07-05' },
    { id: '2', description: 'Aluguel do Apartamento', amount: 2200.00, type: 'expense', category: 'Habitação', date: '2026-07-06' },
    { id: '3', description: 'Supermercado Imperial', amount: 659.50, type: 'expense', category: 'Alimentação', date: '2026-07-07' },
    { id: '4', description: 'Assinatura de Streaming', amount: 55.00, type: 'expense', category: 'Entretenimento', date: '2026-07-07' },
    { id: '5', description: 'Combustível Posto Ipiranga', amount: 345.00, type: 'expense', category: 'Transporte', date: '2026-07-07' },
  ]);

  const categories = [
    'Salário',
    'Habitação',
    'Alimentação',
    'Transporte',
    'Entretenimento',
    'Outros'
  ];

  const handleAddTransaction = (e: React.FormEvent) => {
    e.preventDefault();
    if (!description || !amount || isNaN(Number(amount)) || Number(amount) <= 0) {
      alert('Por favor, preencha a descrição e um valor numérico válido.');
      return;
    }

    const newTransaction: Transaction = {
      id: Math.random().toString(36).substring(2, 9),
      description,
      amount: Number(amount),
      type,
      category,
      date,
    };

    setTransactions((prev) => [newTransaction, ...prev]);
    setIsModalOpen(false);

    // Reset formulário
    setDescription('');
    setAmount('');
    setType('expense');
    setCategory('Alimentação');
    setDate(new Date().toISOString().split('T')[0]);
  };

  const handleDeleteTransaction = async (id: string) => {
    const confirmed = await confirm({
      title: 'Excluir Transação',
      message: 'Tem certeza de que deseja excluir esta transação?',
      type: 'danger',
      confirmText: 'Excluir',
      isBlocking: true,
    });
    if (confirmed) {
      setTransactions((prev) => prev.filter((t) => t.id !== id));
    }
  };

  const filteredTransactions = transactions.filter((t) => {
    const matchesSearch = t.description.toLowerCase().includes(search.toLowerCase()) || 
                          t.category.toLowerCase().includes(search.toLowerCase());
    const matchesType = typeFilter === 'all' ? true : t.type === typeFilter;
    return matchesSearch && matchesType;
  });

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
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
            Transações
          </h2>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium mt-1">
            Gerencie e filtre suas receitas e despesas registradas.
          </p>
        </div>
        <button
          onClick={() => setIsModalOpen(true)}
          className="flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-500 text-white px-5 py-3 rounded-xl font-semibold shadow-lg shadow-indigo-600/10 hover:shadow-indigo-600/20 transition-all self-start"
        >
          <Plus className="w-5 h-5" />
          <span>Nova Transação</span>
        </button>
      </div>

      {/* Grid de Filtros */}
      <div className="bg-white dark:bg-slate-800 p-5 rounded-2xl border border-slate-200 dark:border-slate-700/50 shadow-sm mb-6 flex flex-col md:flex-row gap-4 items-center justify-between">
        {/* Pesquisa */}
        <div className="relative w-full md:max-w-xs">
          <Search className="absolute inset-y-0 left-3 h-5 w-5 text-slate-400 dark:text-slate-500 flex items-center pointer-events-none self-center my-auto" />
          <input
            type="text"
            placeholder="Pesquisar descrição..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="block w-full pl-10 pr-4 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all"
          />
        </div>

        {/* Chaves de Tipo de Transação */}
        <div className="flex bg-slate-50 dark:bg-slate-900 p-1 rounded-xl border border-slate-200 dark:border-slate-700 w-full md:w-auto">
          <button
            onClick={() => setTypeFilter('all')}
            className={`flex-1 md:flex-none px-4 py-2 text-xs font-bold rounded-lg transition-all ${
              typeFilter === 'all'
                ? 'bg-white dark:bg-slate-800 text-slate-900 dark:text-white shadow-sm'
                : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            }`}
          >
            Todas
          </button>
          <button
            onClick={() => setTypeFilter('income')}
            className={`flex-1 md:flex-none px-4 py-2 text-xs font-bold rounded-lg transition-all flex items-center justify-center gap-1.5 ${
              typeFilter === 'income'
                ? 'bg-emerald-500 text-white shadow-sm'
                : 'text-slate-500 dark:text-slate-400 hover:text-emerald-600'
            }`}
          >
            <TrendingUp className="w-3.5 h-3.5" />
            <span>Receitas</span>
          </button>
          <button
            onClick={() => setTypeFilter('expense')}
            className={`flex-1 md:flex-none px-4 py-2 text-xs font-bold rounded-lg transition-all flex items-center justify-center gap-1.5 ${
              typeFilter === 'expense'
                ? 'bg-rose-500 text-white shadow-sm'
                : 'text-slate-500 dark:text-slate-400 hover:text-rose-600'
            }`}
          >
            <TrendingDown className="w-3.5 h-3.5" />
            <span>Despesas</span>
          </button>
        </div>
      </div>

      {/* Lista de Transações */}
      <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 shadow-sm overflow-hidden">
        {/* Tabela de Transações (Desktop) */}
        <div className="hidden md:block overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-slate-100 dark:border-slate-700 bg-slate-50 dark:bg-slate-900/30 text-xs font-bold text-slate-400 dark:text-slate-500 uppercase tracking-wider">
                <th className="px-6 py-4.5">Descrição</th>
                <th className="px-6 py-4.5">Categoria</th>
                <th className="px-6 py-4.5">Data</th>
                <th className="px-6 py-4.5 text-right">Valor</th>
                <th className="px-6 py-4.5 text-right">Ações</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-700/50">
              {filteredTransactions.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-12 text-center text-sm text-slate-500 dark:text-slate-400 font-medium">
                    Nenhuma transação encontrada correspondente aos filtros.
                  </td>
                </tr>
              ) : (
                filteredTransactions.map((t) => (
                  <tr key={t.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-700/10 transition-colors">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div className={`w-9 h-9 rounded-xl flex items-center justify-center ${
                          t.type === 'income' 
                            ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400' 
                            : 'bg-rose-50 dark:bg-rose-950/40 text-rose-600 dark:text-rose-400'
                        }`}>
                          {t.type === 'income' ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                        </div>
                        <span className="font-bold text-sm text-slate-900 dark:text-white">{t.description}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-500 dark:text-slate-400 font-semibold">{t.category}</td>
                    <td className="px-6 py-4 text-sm text-slate-500 dark:text-slate-400 font-medium">{formatDate(t.date)}</td>
                    <td className={`px-6 py-4 text-sm font-extrabold text-right ${
                      t.type === 'income' ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'
                    }`}>
                      {t.type === 'income' ? '+' : '-'} {formatCurrency(t.amount)}
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button
                        onClick={() => handleDeleteTransaction(t.id)}
                        className="text-slate-400 hover:text-red-600 dark:hover:text-red-400 p-2 rounded-lg hover:bg-red-50 dark:hover:bg-red-950/20 transition-colors"
                        title="Excluir Transação"
                      >
                        <Trash2 className="w-4.5 h-4.5" />
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Lista de Transações (Mobile) */}
        <div className="md:hidden divide-y divide-slate-100 dark:divide-slate-700/50">
          {filteredTransactions.length === 0 ? (
            <div className="px-6 py-12 text-center text-sm text-slate-500 dark:text-slate-400">
              Nenhuma transação encontrada correspondente aos filtros.
            </div>
          ) : (
            filteredTransactions.map((t) => (
              <div key={t.id} className="p-4 flex items-center justify-between hover:bg-slate-50/50 dark:hover:bg-slate-700/10">
                <div className="flex items-center gap-3 min-w-0">
                  <div className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 ${
                    t.type === 'income' 
                      ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400' 
                      : 'bg-rose-50 dark:bg-rose-950/40 text-rose-600 dark:text-rose-400'
                  }`}>
                    {t.type === 'income' ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                  </div>
                  <div className="min-w-0">
                    <p className="text-sm font-bold text-slate-900 dark:text-white truncate">{t.description}</p>
                    <div className="flex items-center gap-2 mt-0.5">
                      <span className="text-xs text-slate-400 dark:text-slate-500 font-semibold">{t.category}</span>
                      <span className="w-1 h-1 rounded-full bg-slate-300 dark:bg-slate-600" />
                      <span className="text-xs text-slate-400 dark:text-slate-500 font-medium">{formatDate(t.date)}</span>
                    </div>
                  </div>
                </div>
                <div className="flex items-center gap-3 shrink-0">
                  <span className={`text-sm font-extrabold ${
                    t.type === 'income' ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'
                  }`}>
                    {t.type === 'income' ? '+' : '-'} {formatCurrency(t.amount)}
                  </span>
                  <button
                    onClick={() => handleDeleteTransaction(t.id)}
                    className="text-slate-400 hover:text-red-600 dark:hover:text-red-400 p-2 rounded-lg hover:bg-red-50 dark:hover:bg-red-950/20"
                  >
                    <Trash2 className="w-4.5 h-4.5" />
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Modal / Diálogo para Adição de Transação (Acessibilidade + Design) */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center px-4">
          {/* Backdrop */}
          <div 
            className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm transition-opacity"
            onClick={() => setIsModalOpen(false)}
          />
          {/* Caixa do Modal */}
          <div className="relative bg-white dark:bg-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-100 dark:border-slate-700 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between mb-6">
              <h3 className="font-extrabold text-lg text-slate-900 dark:text-white">Nova Transação</h3>
              <button
                onClick={() => setIsModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-700 p-1.5 rounded-lg transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleAddTransaction} className="space-y-4">
              <div>
                <label className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Tipo de Fluxo
                </label>
                <div className="grid grid-cols-2 gap-3 p-1 bg-slate-50 dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-700">
                  <button
                    type="button"
                    onClick={() => setType('expense')}
                    className={`py-2 text-xs font-bold rounded-lg transition-all ${
                      type === 'expense'
                        ? 'bg-rose-500 text-white shadow-sm'
                        : 'text-slate-500 dark:text-slate-400 hover:text-rose-600'
                    }`}
                  >
                    Despesa
                  </button>
                  <button
                    type="button"
                    onClick={() => setType('income')}
                    className={`py-2 text-xs font-bold rounded-lg transition-all ${
                      type === 'income'
                        ? 'bg-emerald-500 text-white shadow-sm'
                        : 'text-slate-500 dark:text-slate-400 hover:text-emerald-600'
                    }`}
                  >
                    Receita
                  </button>
                </div>
              </div>

              <div>
                <label htmlFor="modal-description" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Descrição
                </label>
                <input
                  id="modal-description"
                  type="text"
                  required
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all"
                  placeholder="Ex: Aluguel, Supermercado..."
                />
              </div>

              <div>
                <label htmlFor="modal-amount" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Valor (R$)
                </label>
                <input
                  id="modal-amount"
                  type="text"
                  required
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all"
                  placeholder="0,00"
                />
              </div>

              <div>
                <label htmlFor="modal-category" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Categoria
                </label>
                <select
                  id="modal-category"
                  value={category}
                  onChange={(e) => setCategory(e.target.value)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all"
                >
                  {categories.map((cat) => (
                    <option key={cat} value={cat}>
                      {cat}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label htmlFor="modal-date" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Data de Movimentação
                </label>
                <input
                  id="modal-date"
                  type="date"
                  required
                  value={date}
                  onChange={(e) => setDate(e.target.value)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all"
                />
              </div>

              <div className="pt-4 flex gap-3">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="flex-1 py-3 px-4 border border-slate-200 dark:border-slate-700 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-700/50 text-sm font-semibold text-slate-700 dark:text-slate-200 transition-all h-11"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  className="flex-1 py-3 px-4 rounded-xl shadow-lg shadow-indigo-600/10 hover:shadow-indigo-600/20 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-500 transition-all h-11"
                >
                  Confirmar
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </Layout>
  );
}
