import { useState, useEffect } from 'react';
import Layout from '@/shared/components/Layout';
import { useConfirm } from '@/shared/context/ConfirmContext';
import { 
  Wallet, 
  Landmark, 
  PiggyBank, 
  Briefcase, 
  Plus, 
  Trash2, 
  Pencil, 
  X, 
  Loader2, 
  AlertCircle 
} from 'lucide-react';
import axios from 'axios';
import { apiClient } from '@/core/api/client';

interface Account {
  id: string;
  name: string;
  type: 'CheckingAccount' | 'SavingsAccount' | 'Investment' | 'Cash';
  initialBalance: number;
  balance: number;
}

export default function AccountsView() {
  const confirm = useConfirm();
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // Estados do Modal de Formulário
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [editingAccountId, setEditingAccountId] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Campos do formulário
  const [name, setName] = useState('');
  const [type, setType] = useState<'CheckingAccount' | 'SavingsAccount' | 'Investment' | 'Cash'>('CheckingAccount');
  const [initialBalance, setInitialBalance] = useState('');

  const fetchAccounts = async (signal?: AbortSignal) => {
    try {
      setIsLoading(true);
      setError('');
      const response = await apiClient.get<Account[]>('/accounts', { signal });
      setAccounts(response.data);
    } catch (err: any) {
      if (axios.isCancel(err)) {
        return;
      }
      console.error('Erro ao buscar contas:', err);
      setError('Não foi possível carregar suas contas. Por favor, verifique sua conexão.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    const controller = new AbortController();
    fetchAccounts(controller.signal);

    return () => {
      controller.abort();
    };
  }, []);

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const getAccountTypeLabel = (accountType: string) => {
    switch (accountType) {
      case 'CheckingAccount': return 'Conta Corrente';
      case 'SavingsAccount': return 'Poupança';
      case 'Investment': return 'Investimento';
      case 'Cash': return 'Dinheiro / Carteira';
      default: return accountType;
    }
  };

  const getAccountTypeStyle = (accountType: string) => {
    switch (accountType) {
      case 'CheckingAccount':
        return {
          icon: Landmark,
          bg: 'bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400',
          border: 'border-blue-100 dark:border-blue-900/30',
          hover: 'hover:border-blue-200 dark:hover:border-blue-800'
        };
      case 'SavingsAccount':
        return {
          icon: PiggyBank,
          bg: 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400',
          border: 'border-emerald-100 dark:border-emerald-900/30',
          hover: 'hover:border-emerald-200 dark:hover:border-emerald-800'
        };
      case 'Investment':
        return {
          icon: Briefcase,
          bg: 'bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400',
          border: 'border-indigo-100 dark:border-indigo-900/30',
          hover: 'hover:border-indigo-200 dark:hover:border-indigo-800'
        };
      case 'Cash':
      default:
        return {
          icon: Wallet,
          bg: 'bg-amber-50 dark:bg-amber-950/40 text-amber-600 dark:text-amber-400',
          border: 'border-amber-100 dark:border-amber-900/30',
          hover: 'hover:border-amber-200 dark:hover:border-amber-800'
        };
    }
  };

  const openCreateModal = () => {
    setModalMode('create');
    setEditingAccountId(null);
    setName('');
    setType('CheckingAccount');
    setInitialBalance('0');
    setIsModalOpen(true);
  };

  const openEditModal = (account: Account) => {
    setModalMode('edit');
    setEditingAccountId(account.id);
    setName(account.name);
    setType(account.type);
    setInitialBalance(account.initialBalance.toString());
    setIsModalOpen(true);
  };

  const handleFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const normalizedName = name.trim().replace(/\s+/g, ' ');

    if (!normalizedName || normalizedName.length < 3) {
      alert('O nome da conta deve conter no mínimo 3 caracteres.');
      return;
    }

    const parsedBalance = parseFloat(initialBalance);
    if (isNaN(parsedBalance)) {
      alert('Insira um valor numérico válido para o saldo inicial.');
      return;
    }

    // Garante precisão decimal de moeda de duas casas decimais
    const balanceValue = parseFloat(parsedBalance.toFixed(2));

    if (type === 'Cash' && balanceValue < 0) {
      alert('Contas do tipo "Dinheiro / Carteira" não podem possuir saldo inicial negativo.');
      return;
    }

    try {
      setIsSubmitting(true);

      const requestData = {
        name: normalizedName,
        type,
        initialBalance: balanceValue
      };

      if (modalMode === 'create') {
        await apiClient.post('/accounts', requestData);
      } else {
        await apiClient.put(`/accounts/${editingAccountId}`, requestData);
      }

      setIsModalOpen(false);
      fetchAccounts();
    } catch (err: any) {
      console.error('Erro ao salvar conta:', err);
      const errorMessage = err.response?.data?.detail || err.response?.data?.message || 'Falha ao salvar a conta financeira.';
      alert(errorMessage);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteAccount = async (id: string, accountName: string) => {
    const confirmationText = `Deseja realmente excluir/arquivar a conta "${accountName}"?\n\nObservação: Caso esta conta possua transações registradas, ela será arquivada (arquivamento lógico) para preservar o histórico e a integridade de seus relatórios financeiros.`;
    const confirmed = await confirm({
      title: 'Excluir ou Arquivar Conta',
      message: confirmationText,
      type: 'warning',
      confirmText: 'Confirmar',
      isBlocking: true,
    });
    if (!confirmed) {
      return;
    }

    try {
      await apiClient.delete(`/accounts/${id}`);
      fetchAccounts();
    } catch (err: any) {
      console.error('Erro ao excluir conta:', err);
      const errorMessage = err.response?.data?.detail || err.response?.data?.message || 'Não foi possível excluir a conta.';
      alert(errorMessage);
    }
  };

  if (isLoading && accounts.length === 0) {
    return (
      <Layout>
        <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
          <Loader2 className="w-12 h-12 text-indigo-600 animate-spin" />
          <p className="text-sm text-slate-500 dark:text-slate-400 font-semibold tracking-wide">
            Carregando contas e carteiras...
          </p>
        </div>
      </Layout>
    );
  }

  if (error && accounts.length === 0) {
    return (
      <Layout>
        <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4 max-w-md mx-auto text-center">
          <div className="w-16 h-16 bg-rose-50 dark:bg-rose-950/30 rounded-2xl flex items-center justify-center text-rose-600 dark:text-rose-400 shadow-md">
            <AlertCircle className="w-8 h-8" />
          </div>
          <h3 className="text-lg font-bold text-slate-900 dark:text-white mt-2">Falha na Conexão</h3>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium leading-relaxed">
            {error}
          </p>
          <button 
            onClick={() => fetchAccounts()}
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
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
            Contas & Carteiras
          </h2>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium mt-1">
            Gerencie suas contas bancárias, poupanças, carteira física ou investimentos.
          </p>
        </div>
        <button
          onClick={openCreateModal}
          className="flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-500 text-white px-5 py-3 rounded-xl font-semibold shadow-lg shadow-indigo-600/10 hover:shadow-indigo-600/20 transition-all self-start"
        >
          <Plus className="w-5 h-5" />
          <span>Nova Conta</span>
        </button>
      </div>

      {/* Grid de Contas */}
      {accounts.length === 0 ? (
        <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 p-12 text-center shadow-sm max-w-xl mx-auto mt-6">
          <div className="w-14 h-14 bg-indigo-50 dark:bg-indigo-950/40 rounded-2xl flex items-center justify-center text-indigo-600 dark:text-indigo-400 mx-auto mb-4">
            <Wallet className="w-7 h-7" />
          </div>
          <h3 className="text-lg font-bold text-slate-900 dark:text-white mb-2">Nenhuma conta cadastrada</h3>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium leading-relaxed mb-6">
            Para gerenciar e categorizar seu dinheiro e transações, comece criando sua primeira conta (ex: Conta Corrente Itaú, Carteira Dinheiro, etc).
          </p>
          <button
            onClick={openCreateModal}
            className="inline-flex items-center gap-2 bg-indigo-600 hover:bg-indigo-500 text-white px-5 py-2.5 rounded-xl font-semibold text-sm transition-all"
          >
            <Plus className="w-4 h-4" />
            <span>Cadastrar Primeira Conta</span>
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {accounts.map((account) => {
            const style = getAccountTypeStyle(account.type);
            const Icon = style.icon;

            return (
              <div 
                key={account.id} 
                className={`bg-white dark:bg-slate-800 p-6 rounded-2xl border ${style.border} ${style.hover} shadow-sm transition-all relative overflow-hidden flex flex-col justify-between h-48`}
              >
                {/* Cabeçalho do Card */}
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className={`w-12 h-12 rounded-xl flex items-center justify-center shadow-inner ${style.bg}`}>
                      <Icon className="w-6 h-6" />
                    </div>
                    <div className="min-w-0">
                      <h4 className="font-extrabold text-slate-900 dark:text-white truncate text-base leading-snug">
                        {account.name}
                      </h4>
                      <span className="text-xs font-semibold text-slate-400 dark:text-slate-500">
                        {getAccountTypeLabel(account.type)}
                      </span>
                    </div>
                  </div>

                  {/* Ações */}
                  <div className="flex items-center gap-1">
                    <button
                      onClick={() => openEditModal(account)}
                      className="text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                      title="Editar Conta"
                    >
                      <Pencil className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => handleDeleteAccount(account.id, account.name)}
                      className="text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 p-2 rounded-lg hover:bg-rose-50 dark:hover:bg-rose-950/20 transition-colors"
                      title="Excluir/Arquivar Conta"
                    >
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>

                {/* Saldo da Conta */}
                <div className="mt-4 pt-4 border-t border-slate-100 dark:border-slate-700/50">
                  <span className="text-xs font-bold text-slate-400 dark:text-slate-500 uppercase tracking-wider block">
                    Saldo
                  </span>
                  <span className={`text-2xl font-extrabold tracking-tight block mt-1 ${
                    account.balance >= 0 
                      ? 'text-slate-950 dark:text-white' 
                      : 'text-rose-600 dark:text-rose-400'
                  }`}>
                    {formatCurrency(account.balance)}
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Modal / Diálogo do Formulário */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center px-4">
          {/* Backdrop */}
          <div 
            className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm transition-opacity"
            onClick={() => !isSubmitting && setIsModalOpen(false)}
          />

          {/* Modal Container */}
          <div className="relative bg-white dark:bg-slate-800 rounded-2xl max-w-md w-full p-6 shadow-2xl border border-slate-100 dark:border-slate-700 animate-in fade-in zoom-in-95 duration-150">
            <div className="flex items-center justify-between mb-6">
              <h3 className="font-extrabold text-lg text-slate-900 dark:text-white">
                {modalMode === 'create' ? 'Nova Conta Financeira' : 'Editar Conta Financeira'}
              </h3>
              {!isSubmitting && (
                <button
                  onClick={() => setIsModalOpen(false)}
                  className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-700 p-1.5 rounded-lg transition-colors"
                >
                  <X className="w-5 h-5" />
                </button>
              )}
            </div>

            <form onSubmit={handleFormSubmit} className="space-y-4">
              {/* Nome */}
              <div>
                <label htmlFor="account-name" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Nome da Conta / Banco
                </label>
                <input
                  id="account-name"
                  type="text"
                  required
                  disabled={isSubmitting}
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all disabled:opacity-50"
                  placeholder="Ex: Itaú Corrente, Nu Poupança, Carteira de Bolso..."
                />
              </div>

              {/* Tipo */}
              <div>
                <label htmlFor="account-type" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Tipo de Conta
                </label>
                <select
                  id="account-type"
                  disabled={isSubmitting}
                  value={type}
                  onChange={(e) => setType(e.target.value as any)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all disabled:opacity-50"
                >
                  <option value="CheckingAccount">Conta Corrente</option>
                  <option value="SavingsAccount">Poupança</option>
                  <option value="Investment">Investimento</option>
                  <option value="Cash">Dinheiro / Carteira Física</option>
                </select>
              </div>

              {/* Saldo Inicial */}
              <div>
                <label htmlFor="account-balance" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Saldo Inicial (R$)
                </label>
                <input
                  id="account-balance"
                  type="number"
                  step="0.01"
                  required
                  disabled={isSubmitting}
                  value={initialBalance}
                  onChange={(e) => setInitialBalance(e.target.value)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all disabled:opacity-50"
                  placeholder="0.00"
                />
                <span className="text-xs text-slate-400 dark:text-slate-500 block mt-1 font-medium">
                  {modalMode === 'edit' 
                    ? 'Aviso: Alterar o saldo inicial só é permitido caso esta conta não possua transações vinculadas.'
                    : 'Insira o saldo atual ou de abertura desta conta.'}
                </span>
              </div>

              {/* Botões */}
              <div className="pt-4 flex gap-3">
                <button
                  type="button"
                  disabled={isSubmitting}
                  onClick={() => setIsModalOpen(false)}
                  className="flex-1 py-3 px-4 border border-slate-200 dark:border-slate-700 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-700/50 text-sm font-semibold text-slate-700 dark:text-slate-200 transition-all h-11 disabled:opacity-50"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="flex-1 py-3 px-4 rounded-xl shadow-lg shadow-indigo-600/10 hover:shadow-indigo-600/20 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-500 transition-all h-11 flex items-center justify-center gap-2 disabled:opacity-50"
                >
                  {isSubmitting ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      <span>Salvando...</span>
                    </>
                  ) : (
                    <span>Confirmar</span>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </Layout>
  );
}
