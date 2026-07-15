import { useState, useEffect } from 'react';
import Layout from '@/shared/components/Layout';
import { useConfirm } from '@/shared/context/ConfirmContext';
import { 
  TrendingDown, 
  Search, 
  Plus, 
  Trash2, 
  X,
  Pencil,
  Loader2,
  ArrowLeftRight,
  ChevronLeft,
  ChevronRight,
  Filter,
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

interface Category {
  id: string;
  name: string;
  flowType: 'Income' | 'Expense' | 'Both';
  isGlobal: boolean;
}

interface Transaction {
  id: string;
  description: string;
  type: 'Income' | 'Expense';
  money: {
    amount: number;
    currency: string;
  };
  accountId: string;
  categoryId: string;
  transactionDate: string;
}

interface Transfer {
  id: string;
  description: string;
  money: {
    amount: number;
    currency: string;
  };
  originAccountId: string;
  destinationAccountId: string;
  categoryId: string;
  transferDate: string;
}

export default function TransactionsView() {
  const confirm = useConfirm();

  // Dados de Suporte (Contas e Categorias)
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [isLoadingSupport, setIsLoadingSupport] = useState(true);

  // Aba principal ativa: 'transactions' ou 'transfers'
  const [activeTab, setActiveTab] = useState<'transactions' | 'transfers'>('transactions');

  // Controle de exibição de filtros no mobile (gaveta/drawer simulado ou inline condensado)
  const [isMobileFilterOpen, setIsMobileFilterOpen] = useState(false);

  // ==========================================
  // ESTADO DA ABA DE TRANSAÇÕES
  // ==========================================
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [transactionsPage, setTransactionsPage] = useState(1);
  const [transactionsFilterAccount, setTransactionsFilterAccount] = useState('');
  const [transactionsFilterCategory, setTransactionsFilterCategory] = useState('');
  const [transactionsSearch, setTransactionsSearch] = useState('');
  const [isLoadingTransactions, setIsLoadingTransactions] = useState(false);
  const [transactionsHasNext, setTransactionsHasNext] = useState(false);

  // ==========================================
  // ESTADO DA ABA DE TRANSFERÊNCIAS
  // ==========================================
  const [transfers, setTransfers] = useState<Transfer[]>([]);
  const [transfersPage, setTransfersPage] = useState(1);
  const [transfersFilterAccount, setTransfersFilterAccount] = useState('');
  const [transfersSearch, setTransfersSearch] = useState('');
  const [isLoadingTransfers, setIsLoadingTransfers] = useState(false);
  const [transfersHasNext, setTransfersHasNext] = useState(false);

  // ==========================================
  // ESTADOS DO MODAL FORMULÁRIO UNIFICADO
  // ==========================================
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [editingItemId, setEditingItemId] = useState<string | null>(null);
  const [modalTab, setModalTab] = useState<'transaction' | 'transfer'>('transaction');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Campos compartilhados e específicos
  const [formDescription, setFormDescription] = useState('');
  const [formAmount, setFormAmount] = useState('');
  const [formDate, setFormDate] = useState(new Date().toISOString().split('T')[0]);
  
  // Campos específicos de transações
  const [formType, setFormType] = useState<'Income' | 'Expense'>('Expense');
  const [formAccountId, setFormAccountId] = useState('');
  const [formCategoryId, setFormCategoryId] = useState('');

  // Campos específicos de transferências
  const [formOriginAccountId, setFormOriginAccountId] = useState('');
  const [formDestinationAccountId, setFormDestinationAccountId] = useState('');

  // ==========================================
  // BUSCA DE DADOS DE SUPORTE (INITIAL LOAD)
  // ==========================================
  useEffect(() => {
    const controller = new AbortController();

    const fetchSupportData = async () => {
      try {
        setIsLoadingSupport(true);
        const [accountsRes, categoriesRes] = await Promise.all([
          apiClient.get<Account[]>('/accounts', { signal: controller.signal }),
          apiClient.get<Category[]>('/categories', { signal: controller.signal })
        ]);
        
        setAccounts(accountsRes.data);
        setCategories(categoriesRes.data);

        // Inicializa seletores padrão se houver dados
        if (accountsRes.data.length > 0) {
          setFormAccountId(accountsRes.data[0].id);
          setFormOriginAccountId(accountsRes.data[0].id);
          if (accountsRes.data.length > 1) {
            setFormDestinationAccountId(accountsRes.data[1].id);
          } else {
            setFormDestinationAccountId(accountsRes.data[0].id);
          }
        }
      } catch (err) {
        if (!axios.isCancel(err)) {
          console.error('Erro ao buscar dados de suporte:', err);
        }
      } finally {
        setIsLoadingSupport(false);
      }
    };

    fetchSupportData();

    return () => {
      controller.abort();
    };
  }, []);

  // ==========================================
  // BUSCA DE TRANSAÇÕES (PAGINADA)
  // ==========================================
  const fetchTransactions = async (page: number, signal?: AbortSignal) => {
    try {
      setIsLoadingTransactions(true);
      const params = new URLSearchParams();
      params.append('page', page.toString());
      params.append('pageSize', '20');
      if (transactionsFilterAccount) {
        params.append('accountId', transactionsFilterAccount);
      }
      if (transactionsFilterCategory) {
        params.append('categoryId', transactionsFilterCategory);
      }

      const response = await apiClient.get<Transaction[]>('/transactions', {
        params,
        signal
      });

      setTransactions(response.data);
      // Se retornou 20 itens, assumimos que possa haver uma próxima página
      setTransactionsHasNext(response.data.length === 20);
    } catch (err) {
      if (!axios.isCancel(err)) {
        console.error('Erro ao buscar transações:', err);
      }
    } finally {
      setIsLoadingTransactions(false);
    }
  };

  useEffect(() => {
    if (activeTab === 'transactions') {
      const controller = new AbortController();
      fetchTransactions(transactionsPage, controller.signal);
      return () => {
        controller.abort();
      };
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [transactionsPage, transactionsFilterAccount, transactionsFilterCategory, activeTab]);

  // ==========================================
  // BUSCA DE TRANSFERÊNCIAS (PAGINADA)
  // ==========================================
  const fetchTransfers = async (page: number, signal?: AbortSignal) => {
    try {
      setIsLoadingTransfers(true);
      const params = new URLSearchParams();
      params.append('page', page.toString());
      params.append('pageSize', '20');

      const response = await apiClient.get<Transfer[]>('/transfers', {
        params,
        signal
      });

      setTransfers(response.data);
      setTransfersHasNext(response.data.length === 20);
    } catch (err) {
      if (!axios.isCancel(err)) {
        console.error('Erro ao buscar transferências:', err);
      }
    } finally {
      setIsLoadingTransfers(false);
    }
  };

  useEffect(() => {
    if (activeTab === 'transfers') {
      const controller = new AbortController();
      fetchTransfers(transfersPage, controller.signal);
      return () => {
        controller.abort();
      };
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [transfersPage, activeTab]);

  // Reset de paginação ao alterar filtros
  const handleTransactionsFilterAccountChange = (val: string) => {
    setTransactionsFilterAccount(val);
    setTransactionsPage(1);
  };

  const handleTransactionsFilterCategoryChange = (val: string) => {
    setTransactionsFilterCategory(val);
    setTransactionsPage(1);
  };

  const handleTransfersFilterAccountChange = (val: string) => {
    setTransfersFilterAccount(val);
    setTransfersPage(1);
  };

  // ==========================================
  // FILTRAGEM LOCAL DE BUSCA (SEARCH)
  // ==========================================
  const filteredTransactions = transactions.filter(t => {
    if (!transactionsSearch.trim()) return true;
    const catName = categories.find(c => c.id === t.categoryId)?.name || '';
    return (
      t.description.toLowerCase().includes(transactionsSearch.toLowerCase()) ||
      catName.toLowerCase().includes(transactionsSearch.toLowerCase())
    );
  });

  const filteredTransfers = transfers.filter(t => {
    // Filtro por busca de texto (descrição)
    const matchesSearch = !transfersSearch.trim() || t.description.toLowerCase().includes(transfersSearch.toLowerCase());
    
    // Filtro local por conta (uma vez que o endpoint não aceita accountId no backend)
    const matchesAccount = !transfersFilterAccount || 
                           t.originAccountId === transfersFilterAccount || 
                           t.destinationAccountId === transfersFilterAccount;

    return matchesSearch && matchesAccount;
  });

  // ==========================================
  // PROCESSAMENTO DE FORMULÁRIO (SUBMIT)
  // ==========================================
  const handleFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // 1. Validações Locais Comuns
    const cleanedDescription = formDescription.trim().replace(/\s+/g, ' ');
    if (cleanedDescription.length < 3 || cleanedDescription.length > 250) {
      alert('A descrição deve conter entre 3 e 250 caracteres.');
      return;
    }

    const numericAmount = parseFloat(formAmount);
    if (isNaN(numericAmount) || numericAmount <= 0) {
      alert('O valor monetário deve ser maior que zero.');
      return;
    }

    if (numericAmount > 99999999999.99) {
      alert('O valor máximo permitido é R$ 99.999.999.999,99.');
      return;
    }

    // Limite de Ano temporal entre 2000 e 2100
    const selectedDate = new Date(formDate);
    const selectedYear = selectedDate.getFullYear();
    if (isNaN(selectedYear) || selectedYear < 2000 || selectedYear > 2100) {
      alert('O ano da movimentação deve ser um valor contábil entre 2000 e 2100.');
      return;
    }

    // Conversão de Data para ISO UTC preservando o horário atual local
    const now = new Date();
    const localDateWithTime = new Date(
      `${formDate}T${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:${String(now.getSeconds()).padStart(2, '0')}`
    );
    const transactionDateUtc = localDateWithTime.toISOString();

    try {
      setIsSubmitting(true);

      if (modalTab === 'transaction') {
        // Validação de Conta e Categoria
        if (!formAccountId) {
          alert('Selecione uma conta financeira.');
          setIsSubmitting(false);
          return;
        }
        if (!formCategoryId) {
          alert('Selecione uma categoria.');
          setIsSubmitting(false);
          return;
        }

        const payload = {
          description: cleanedDescription,
          type: formType,
          amount: numericAmount,
          currency: 'BRL',
          accountId: formAccountId,
          categoryId: formCategoryId,
          transactionDate: transactionDateUtc
        };

        if (modalMode === 'create') {
          await apiClient.post('/transactions', payload);
        } else {
          await apiClient.put(`/transactions/${editingItemId}`, payload);
        }
      } else {
        // Validação de Transferência
        if (!formOriginAccountId || !formDestinationAccountId) {
          alert('Selecione as contas de origem e destino.');
          setIsSubmitting(false);
          return;
        }
        if (formOriginAccountId === formDestinationAccountId) {
          alert('A conta de origem e a conta de destino devem ser obrigatoriamente diferentes.');
          setIsSubmitting(false);
          return;
        }

        const payload = {
          description: cleanedDescription,
          amount: numericAmount,
          currency: 'BRL',
          originAccountId: formOriginAccountId,
          destinationAccountId: formDestinationAccountId,
          transferDate: transactionDateUtc
        };

        if (modalMode === 'create') {
          await apiClient.post('/transfers', payload);
        } else {
          await apiClient.put(`/transfers/${editingItemId}`, payload);
        }
      }

      setIsModalOpen(false);
      resetForm();

      // Recarrega listagens
      if (activeTab === 'transactions') {
        fetchTransactions(transactionsPage);
      } else {
        fetchTransfers(transfersPage);
      }
    } catch (err: any) {
      console.error('Erro ao salvar movimentação:', err);
      const errMsg = err.response?.data?.detail || err.response?.data?.message || 'Falha ao registrar a operação.';
      alert(errMsg);
    } finally {
      setIsSubmitting(false);
    }
  };

  // ==========================================
  // CONFIGURAÇÃO DOS FILTROS DE CATEGORIA DO FORM
  // ==========================================
  const formFilteredCategories = categories.filter(cat => {
    if (formType === 'Expense') {
      return cat.flowType === 'Expense' || cat.flowType === 'Both';
    } else {
      return cat.flowType === 'Income' || cat.flowType === 'Both';
    }
  });

  // Re-ajusta categoria selecionada se ela se tornar inválida devido ao tipo
  useEffect(() => {
    if (isModalOpen && modalTab === 'transaction') {
      const isCurrentCategoryValid = formFilteredCategories.some(c => c.id === formCategoryId);
      if (!isCurrentCategoryValid && formFilteredCategories.length > 0) {
        setFormCategoryId(formFilteredCategories[0].id);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formType, modalTab, isModalOpen, categories]);

  // ==========================================
  // OPERAÇÕES DE EXCLUSÃO (SOFT-DELETE)
  // ==========================================
  const handleDeleteTransaction = async (id: string, description: string) => {
    const confirmed = await confirm({
      title: 'Excluir Transação',
      message: `Deseja realmente excluir a transação "${description}"?\n\nEsta ação reajustará o saldo das contas vinculadas automaticamente no sistema.`,
      type: 'danger',
      confirmText: 'Excluir',
      isBlocking: true
    });

    if (confirmed) {
      try {
        await apiClient.delete(`/transactions/${id}`);
        fetchTransactions(transactionsPage);
      } catch (err: any) {
        console.error('Erro ao excluir transação:', err);
        alert(err.response?.data?.detail || 'Erro ao excluir a transação.');
      }
    }
  };

  const handleDeleteTransfer = async (id: string, description: string) => {
    const confirmed = await confirm({
      title: 'Excluir Transferência',
      message: `Deseja realmente excluir a transferência "${description}"?\n\nO saldo de ambas as contas envolvidas (origem e destino) será revertido em tempo real.`,
      type: 'danger',
      confirmText: 'Excluir',
      isBlocking: true
    });

    if (confirmed) {
      try {
        await apiClient.delete(`/transfers/${id}`);
        fetchTransfers(transfersPage);
      } catch (err: any) {
        console.error('Erro ao excluir transferência:', err);
        alert(err.response?.data?.detail || 'Erro ao excluir a transferência.');
      }
    }
  };

  // ==========================================
  // PREENCHIMENTO E ABERTURA PARA EDIÇÃO
  // ==========================================
  const handleOpenCreateModal = () => {
    setModalMode('create');
    setEditingItemId(null);
    setModalTab(activeTab === 'transactions' ? 'transaction' : 'transfer');
    resetForm();
    setIsModalOpen(true);
  };

  const handleOpenEditTransaction = (t: Transaction) => {
    setModalMode('edit');
    setEditingItemId(t.id);
    setModalTab('transaction');
    setFormDescription(t.description);
    setFormAmount(t.money.amount.toString());
    setFormDate(toLocalDateInputValue(t.transactionDate));
    setFormType(t.type);
    setFormAccountId(t.accountId);
    setFormCategoryId(t.categoryId);
    setIsModalOpen(true);
  };

  const handleOpenEditTransfer = (t: Transfer) => {
    setModalMode('edit');
    setEditingItemId(t.id);
    setModalTab('transfer');
    setFormDescription(t.description);
    setFormAmount(t.money.amount.toString());
    setFormDate(toLocalDateInputValue(t.transferDate));
    setFormOriginAccountId(t.originAccountId);
    setFormDestinationAccountId(t.destinationAccountId);
    setIsModalOpen(true);
  };

  const resetForm = () => {
    setFormDescription('');
    setFormAmount('');
    setFormDate(new Date().toISOString().split('T')[0]);
    setFormType('Expense');
    if (accounts.length > 0) {
      setFormAccountId(accounts[0].id);
      setFormOriginAccountId(accounts[0].id);
      if (accounts.length > 1) {
        setFormDestinationAccountId(accounts[1].id);
      } else {
        setFormDestinationAccountId(accounts[0].id);
      }
    }
    if (categories.length > 0) {
      const firstExpenseCat = categories.find(c => c.flowType === 'Expense' || c.flowType === 'Both');
      setFormCategoryId(firstExpenseCat ? firstExpenseCat.id : categories[0].id);
    }
  };

  // ==========================================
  // UTILS DE FORMATANDO E CONVERSÃO
  // ==========================================
  const toLocalDateInputValue = (dateIsoStr: string) => {
    const d = new Date(dateIsoStr);
    if (isNaN(d.getTime())) return '';
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  };

  const formatDateStr = (dateIsoStr: string) => {
    const d = new Date(dateIsoStr);
    if (isNaN(d.getTime())) return '';
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}/${month}/${year}`;
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL',
    }).format(value);
  };

  const getAccountName = (id: string) => {
    return accounts.find(a => a.id === id)?.name || 'Conta desconhecida';
  };

  const getCategoryName = (id: string) => {
    return categories.find(c => c.id === id)?.name || 'Sem Categoria';
  };

  return (
    <Layout>
      {/* Cabeçalho Superior */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl md:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
            Movimentações Financeiras
          </h2>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium mt-1">
            Controle suas despesas, receitas e transferências de saldos entre contas de forma integrada.
          </p>
        </div>
        <button
          onClick={handleOpenCreateModal}
          className="flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-500 text-white px-5 py-3 rounded-xl font-semibold shadow-lg shadow-indigo-600/10 hover:shadow-indigo-600/20 transition-all self-start shrink-0"
        >
          <Plus className="w-5 h-5" />
          <span>Nova Lançamento</span>
        </button>
      </div>

      {/* Seletor de Abas Principais */}
      <div className="flex bg-slate-100 dark:bg-slate-900 p-1 rounded-2xl border border-slate-200 dark:border-slate-800/80 mb-6 max-w-md">
        <button
          onClick={() => setActiveTab('transactions')}
          className={`flex-1 flex items-center justify-center gap-2 py-3 px-4 rounded-xl text-sm font-bold transition-all ${
            activeTab === 'transactions'
              ? 'bg-white dark:bg-slate-800 text-indigo-600 dark:text-white shadow-md'
              : 'text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
          }`}
        >
          <TrendingDown className="w-4 h-4 shrink-0" />
          <span>Transações</span>
        </button>
        <button
          onClick={() => setActiveTab('transfers')}
          className={`flex-1 flex items-center justify-center gap-2 py-3 px-4 rounded-xl text-sm font-bold transition-all ${
            activeTab === 'transfers'
              ? 'bg-white dark:bg-slate-800 text-indigo-600 dark:text-white shadow-md'
              : 'text-slate-500 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
          }`}
        >
          <ArrowLeftRight className="w-4 h-4 shrink-0" />
          <span>Transferências</span>
        </button>
      </div>

      {/* Seção Principal de Filtros e Listagem */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6 items-start">
        
        {/* Painel de Filtros (Desktop: Lateral Esquerda | Mobile: Gaveta Condensada) */}
        <div className={`lg:block lg:col-span-1 space-y-6 ${isMobileFilterOpen ? 'block' : 'hidden'}`}>
          <div className="bg-white dark:bg-slate-800 p-6 rounded-2xl border border-slate-200 dark:border-slate-700/50 shadow-sm space-y-6">
            <div className="flex items-center justify-between border-b border-slate-100 dark:border-slate-700/50 pb-4">
              <h3 className="font-extrabold text-sm text-slate-900 dark:text-white uppercase tracking-wider">Filtros</h3>
              <button 
                onClick={() => setIsMobileFilterOpen(false)}
                className="lg:hidden text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {activeTab === 'transactions' ? (
              <>
                {/* Filtro por Conta */}
                <div className="space-y-2">
                  <label htmlFor="filter-account" className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider block">
                    Filtrar por Conta
                  </label>
                  <select
                    id="filter-account"
                    value={transactionsFilterAccount}
                    onChange={(e) => handleTransactionsFilterAccountChange(e.target.value)}
                    className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all"
                  >
                    <option value="">Todas as Contas</option>
                    {accounts.map(acc => (
                      <option key={acc.id} value={acc.id}>{acc.name}</option>
                    ))}
                  </select>
                </div>

                {/* Filtro por Categoria */}
                <div className="space-y-2">
                  <label htmlFor="filter-category" className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider block">
                    Filtrar por Categoria
                  </label>
                  <select
                    id="filter-category"
                    value={transactionsFilterCategory}
                    onChange={(e) => handleTransactionsFilterCategoryChange(e.target.value)}
                    className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all"
                  >
                    <option value="">Todas as Categorias</option>
                    {categories.map(cat => (
                      <option key={cat.id} value={cat.id}>{cat.name}</option>
                    ))}
                  </select>
                </div>
              </>
            ) : (
              <>
                {/* Filtro de Conta (Origem ou Destino) para Transferências */}
                <div className="space-y-2">
                  <label htmlFor="filter-transfer-account" className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider block">
                    Filtrar por Conta Envolvida
                  </label>
                  <select
                    id="filter-transfer-account"
                    value={transfersFilterAccount}
                    onChange={(e) => handleTransfersFilterAccountChange(e.target.value)}
                    className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all"
                  >
                    <option value="">Todas as Contas</option>
                    {accounts.map(acc => (
                      <option key={acc.id} value={acc.id}>{acc.name}</option>
                    ))}
                  </select>
                </div>
              </>
            )}

            {/* Botão para limpar filtros */}
            <button
              onClick={() => {
                if (activeTab === 'transactions') {
                  setTransactionsFilterAccount('');
                  setTransactionsFilterCategory('');
                  setTransactionsPage(1);
                } else {
                  setTransfersFilterAccount('');
                  setTransfersPage(1);
                }
              }}
              className="w-full py-2 px-4 border border-slate-200 dark:border-slate-700 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-700/50 text-xs font-bold text-slate-500 dark:text-slate-400 transition-all text-center"
            >
              Limpar Filtros
            </button>
          </div>
        </div>

        {/* Listagem Central à Direita */}
        <div className="lg:col-span-3 space-y-4">
          
          {/* Barra de Pesquisa & Mobile Filter Trigger */}
          <div className="flex gap-3 items-center justify-between">
            <div className="relative flex-1">
              <Search className="absolute inset-y-0 left-3.5 h-5 w-5 text-slate-400 dark:text-slate-500 flex items-center pointer-events-none self-center my-auto" />
              <input
                type="text"
                placeholder="Pesquisar descrição..."
                value={activeTab === 'transactions' ? transactionsSearch : transfersSearch}
                onChange={(e) => activeTab === 'transactions' ? setTransactionsSearch(e.target.value) : setTransfersSearch(e.target.value)}
                className="block w-full pl-11 pr-4 py-3 border border-slate-200 dark:border-slate-700 rounded-2xl bg-white dark:bg-slate-800 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all shadow-sm"
              />
            </div>
            <button
              onClick={() => setIsMobileFilterOpen(!isMobileFilterOpen)}
              className="lg:hidden flex items-center gap-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 p-3 rounded-2xl text-slate-700 dark:text-slate-300 shadow-sm"
              title="Filtros"
            >
              <Filter className="w-5 h-5" />
            </button>
          </div>

          {/* Estado de Carregamento Principal */}
          {isLoadingSupport ? (
            <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 p-12 text-center flex flex-col items-center justify-center gap-4">
              <Loader2 className="w-10 h-10 text-indigo-600 animate-spin" />
              <p className="text-sm font-semibold text-slate-500 dark:text-slate-400">Carregando dados financeiros...</p>
            </div>
          ) : activeTab === 'transactions' ? (
            
            // ==========================================
            // LISTA DA ABA DE TRANSAÇÕES
            // ==========================================
            <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 shadow-sm overflow-hidden">
              {isLoadingTransactions ? (
                <div className="p-16 text-center flex flex-col items-center justify-center gap-4">
                  <Loader2 className="w-8 h-8 text-indigo-600 animate-spin" />
                  <p className="text-sm font-semibold text-slate-400 dark:text-slate-500">Buscando transações...</p>
                </div>
              ) : filteredTransactions.length === 0 ? (
                <div className="p-16 text-center flex flex-col items-center justify-center gap-4">
                  <TrendingDown className="w-12 h-12 text-slate-300 dark:text-slate-600" />
                  <h3 className="text-base font-bold text-slate-800 dark:text-slate-200">Nenhuma transação registrada</h3>
                  <p className="text-sm text-slate-500 dark:text-slate-400 max-w-xs mx-auto font-medium">
                    Não encontramos transações com os filtros ativos. Clique em "Nova Lançamento" para iniciar.
                  </p>
                </div>
              ) : (
                <>
                  {/* Tabela Desktop */}
                  <div className="hidden md:block overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                      <thead>
                        <tr className="border-b border-slate-100 dark:border-slate-700/50 bg-slate-50/50 dark:bg-slate-900/10 text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-wider">
                          <th className="px-6 py-4">Data</th>
                          <th className="px-6 py-4">Descrição</th>
                          <th className="px-6 py-4">Categoria</th>
                          <th className="px-6 py-4">Conta</th>
                          <th className="px-6 py-4">Tipo</th>
                          <th className="px-6 py-4 text-right">Valor</th>
                          <th className="px-6 py-4 text-right">Ações</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 dark:divide-slate-700/40">
                        {filteredTransactions.map(t => (
                          <tr key={t.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-700/10 transition-colors">
                            <td className="px-6 py-4.5 text-xs text-slate-500 dark:text-slate-400 font-bold whitespace-nowrap">
                              {formatDateStr(t.transactionDate)}
                            </td>
                            <td className="px-6 py-4.5">
                              <span className="font-extrabold text-sm text-slate-900 dark:text-white leading-tight block">
                                {t.description}
                              </span>
                            </td>
                            <td className="px-6 py-4.5 whitespace-nowrap text-xs text-slate-500 dark:text-slate-400 font-semibold">
                              {getCategoryName(t.categoryId)}
                            </td>
                            <td className="px-6 py-4.5 whitespace-nowrap text-xs text-slate-500 dark:text-slate-400 font-semibold">
                              {getAccountName(t.accountId)}
                            </td>
                            <td className="px-6 py-4.5 whitespace-nowrap">
                              <span className={`inline-flex items-center gap-1 text-[10px] font-extrabold uppercase px-2 py-0.5 rounded-full ${
                                t.type === 'Income' 
                                  ? 'bg-emerald-50 dark:bg-emerald-950/30 text-emerald-600 dark:text-emerald-400' 
                                  : 'bg-rose-50 dark:bg-rose-950/30 text-rose-600 dark:text-rose-400'
                              }`}>
                                {t.type === 'Income' ? 'Receita' : 'Despesa'}
                              </span>
                            </td>
                            <td className={`px-6 py-4.5 text-right font-extrabold text-sm whitespace-nowrap ${
                              t.type === 'Income' ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'
                            }`}>
                              {t.type === 'Income' ? '+' : '-'} {formatCurrency(t.money.amount)}
                            </td>
                            <td className="px-6 py-4.5 text-right">
                              <div className="flex items-center justify-end gap-1">
                                <button
                                  onClick={() => handleOpenEditTransaction(t)}
                                  className="text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors"
                                  title="Editar"
                                >
                                  <Pencil className="w-4 h-4" />
                                </button>
                                <button
                                  onClick={() => handleDeleteTransaction(t.id, t.description)}
                                  className="text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 p-2 rounded-lg hover:bg-rose-50 dark:hover:bg-rose-950/20 transition-colors"
                                  title="Excluir"
                                >
                                  <Trash2 className="w-4 h-4" />
                                </button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  {/* Lista Mobile */}
                  <div className="md:hidden divide-y divide-slate-100 dark:divide-slate-700/40">
                    {filteredTransactions.map(t => (
                      <div key={t.id} className="p-4 flex flex-col gap-3">
                        <div className="flex items-start justify-between gap-2">
                          <div className="min-w-0">
                            <span className="font-extrabold text-sm text-slate-900 dark:text-white leading-tight block truncate">
                              {t.description}
                            </span>
                            <div className="flex items-center gap-2 mt-1 flex-wrap">
                              <span className="text-[10px] text-slate-400 dark:text-slate-500 font-bold">
                                {formatDateStr(t.transactionDate)}
                              </span>
                              <span className="w-1 h-1 rounded-full bg-slate-300 dark:bg-slate-700" />
                              <span className="text-[10px] text-slate-400 dark:text-slate-500 font-bold truncate max-w-[80px]">
                                {getCategoryName(t.categoryId)}
                              </span>
                              <span className="w-1 h-1 rounded-full bg-slate-300 dark:bg-slate-700" />
                              <span className="text-[10px] text-slate-400 dark:text-slate-500 font-bold truncate max-w-[80px]">
                                {getAccountName(t.accountId)}
                              </span>
                            </div>
                          </div>
                          <span className={`text-sm font-extrabold shrink-0 whitespace-nowrap ${
                            t.type === 'Income' ? 'text-emerald-600 dark:text-emerald-400' : 'text-rose-600 dark:text-rose-400'
                          }`}>
                            {t.type === 'Income' ? '+' : '-'} {formatCurrency(t.money.amount)}
                          </span>
                        </div>
                        <div className="flex justify-between items-center bg-slate-50/50 dark:bg-slate-900/20 px-3 py-1.5 rounded-xl border border-slate-100 dark:border-slate-800/60">
                          <span className={`inline-flex items-center text-[9px] font-extrabold uppercase px-2 py-0.5 rounded-full ${
                            t.type === 'Income' 
                              ? 'bg-emerald-50 dark:bg-emerald-950/30 text-emerald-600 dark:text-emerald-400' 
                              : 'bg-rose-50 dark:bg-rose-950/30 text-rose-600 dark:text-rose-400'
                          }`}>
                            {t.type === 'Income' ? 'Receita' : 'Despesa'}
                          </span>
                          <div className="flex items-center gap-1">
                            <button
                              onClick={() => handleOpenEditTransaction(t)}
                              className="text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700"
                            >
                              <Pencil className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => handleDeleteTransaction(t.id, t.description)}
                              className="text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 p-2 rounded-lg hover:bg-rose-50 dark:hover:bg-rose-950/20"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>

                  {/* Paginação */}
                  <div className="flex items-center justify-between px-6 py-4 bg-slate-50 dark:bg-slate-900/10 border-t border-slate-100 dark:border-slate-700/50">
                    <button
                      onClick={() => setTransactionsPage(prev => Math.max(prev - 1, 1))}
                      disabled={transactionsPage === 1 || isLoadingTransactions}
                      className="flex items-center justify-center gap-1 px-3 py-2 border border-slate-200 dark:border-slate-700 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-700/50 text-xs font-bold text-slate-600 dark:text-slate-400 disabled:opacity-40 transition-all cursor-pointer"
                    >
                      <ChevronLeft className="w-4 h-4" />
                      <span>Anterior</span>
                    </button>
                    <span className="text-xs font-bold text-slate-500 dark:text-slate-400 tracking-wider">
                      Página {transactionsPage}
                    </span>
                    <button
                      onClick={() => setTransactionsPage(prev => prev + 1)}
                      disabled={!transactionsHasNext || isLoadingTransactions}
                      className="flex items-center justify-center gap-1 px-3 py-2 border border-slate-200 dark:border-slate-700 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-700/50 text-xs font-bold text-slate-600 dark:text-slate-400 disabled:opacity-40 transition-all cursor-pointer"
                    >
                      <span>Próxima</span>
                      <ChevronRight className="w-4 h-4" />
                    </button>
                  </div>
                </>
              )}
            </div>
          ) : (
            
            // ==========================================
            // LISTA DA ABA DE TRANSFERÊNCIAS
            // ==========================================
            <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 shadow-sm overflow-hidden">
              {isLoadingTransfers ? (
                <div className="p-16 text-center flex flex-col items-center justify-center gap-4">
                  <Loader2 className="w-8 h-8 text-indigo-600 animate-spin" />
                  <p className="text-sm font-semibold text-slate-400 dark:text-slate-500">Buscando transferências...</p>
                </div>
              ) : filteredTransfers.length === 0 ? (
                <div className="p-16 text-center flex flex-col items-center justify-center gap-4">
                  <ArrowLeftRight className="w-12 h-12 text-slate-300 dark:text-slate-600" />
                  <h3 className="text-base font-bold text-slate-800 dark:text-slate-200">Nenhuma transferência registrada</h3>
                  <p className="text-sm text-slate-500 dark:text-slate-400 max-w-xs mx-auto font-medium">
                    Não encontramos transferências registradas no sistema. Clique em "Nova Lançamento" para registrar.
                  </p>
                </div>
              ) : (
                <>
                  {/* Tabela Desktop */}
                  <div className="hidden md:block overflow-x-auto">
                    <table className="w-full text-left border-collapse">
                      <thead>
                        <tr className="border-b border-slate-100 dark:border-slate-700/50 bg-slate-50/50 dark:bg-slate-900/10 text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-wider">
                          <th className="px-6 py-4">Data</th>
                          <th className="px-6 py-4">Descrição</th>
                          <th className="px-6 py-4">Origem ➔ Destino</th>
                          <th className="px-6 py-4 text-right">Valor</th>
                          <th className="px-6 py-4 text-right">Ações</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 dark:divide-slate-700/40">
                        {filteredTransfers.map(t => (
                          <tr key={t.id} className="hover:bg-slate-50/50 dark:hover:bg-slate-700/10 transition-colors">
                            <td className="px-6 py-4.5 text-xs text-slate-500 dark:text-slate-400 font-bold whitespace-nowrap">
                              {formatDateStr(t.transferDate)}
                            </td>
                            <td className="px-6 py-4.5">
                              <span className="font-extrabold text-sm text-slate-900 dark:text-white leading-tight block">
                                {t.description}
                              </span>
                            </td>
                            <td className="px-6 py-4.5 whitespace-nowrap">
                              <div className="flex items-center gap-2 text-xs font-semibold text-slate-500 dark:text-slate-400">
                                <span className="bg-slate-100 dark:bg-slate-900 px-2 py-1 rounded-lg text-[10px] font-bold text-slate-600 dark:text-slate-300">
                                  {getAccountName(t.originAccountId)}
                                </span>
                                <span className="text-slate-400">➔</span>
                                <span className="bg-indigo-50 dark:bg-indigo-950/20 px-2 py-1 rounded-lg text-[10px] font-bold text-indigo-600 dark:text-indigo-400">
                                  {getAccountName(t.destinationAccountId)}
                                </span>
                              </div>
                            </td>
                            <td className="px-6 py-4.5 text-right font-extrabold text-sm whitespace-nowrap text-slate-600 dark:text-slate-300">
                              {formatCurrency(t.money.amount)}
                            </td>
                            <td className="px-6 py-4.5 text-right">
                              <div className="flex items-center justify-end gap-1">
                                <button
                                  onClick={() => handleOpenEditTransfer(t)}
                                  className="text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors"
                                  title="Editar"
                                >
                                  <Pencil className="w-4 h-4" />
                                </button>
                                <button
                                  onClick={() => handleDeleteTransfer(t.id, t.description)}
                                  className="text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 p-2 rounded-lg hover:bg-rose-50 dark:hover:bg-rose-950/20 transition-colors"
                                  title="Excluir"
                                >
                                  <Trash2 className="w-4 h-4" />
                                </button>
                              </div>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  {/* Lista Mobile */}
                  <div className="md:hidden divide-y divide-slate-100 dark:divide-slate-700/40">
                    {filteredTransfers.map(t => (
                      <div key={t.id} className="p-4 flex flex-col gap-3">
                        <div className="flex items-start justify-between gap-2">
                          <div className="min-w-0">
                            <span className="font-extrabold text-sm text-slate-900 dark:text-white leading-tight block truncate">
                              {t.description}
                            </span>
                            <span className="text-[10px] text-slate-400 dark:text-slate-500 font-bold block mt-1">
                              {formatDateStr(t.transferDate)}
                            </span>
                          </div>
                          <span className="text-sm font-extrabold shrink-0 whitespace-nowrap text-slate-600 dark:text-slate-300">
                            {formatCurrency(t.money.amount)}
                          </span>
                        </div>
                        
                        <div className="flex items-center gap-2 text-[10px] font-bold text-slate-500 dark:text-slate-400 bg-slate-50/50 dark:bg-slate-900/15 p-2 rounded-xl border border-slate-100 dark:border-slate-800">
                          <span className="truncate max-w-[80px]">{getAccountName(t.originAccountId)}</span>
                          <span className="text-slate-400">➔</span>
                          <span className="truncate max-w-[80px] text-indigo-600 dark:text-indigo-400">{getAccountName(t.destinationAccountId)}</span>
                        </div>

                        <div className="flex justify-end items-center gap-1 pt-1">
                          <button
                            onClick={() => handleOpenEditTransfer(t)}
                            className="text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700"
                          >
                            <Pencil className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => handleDeleteTransfer(t.id, t.description)}
                            className="text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 p-2 rounded-lg hover:bg-rose-50 dark:hover:bg-rose-950/20"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>

                  {/* Paginação */}
                  <div className="flex items-center justify-between px-6 py-4 bg-slate-50 dark:bg-slate-900/10 border-t border-slate-100 dark:border-slate-700/50">
                    <button
                      onClick={() => setTransfersPage(prev => Math.max(prev - 1, 1))}
                      disabled={transfersPage === 1 || isLoadingTransfers}
                      className="flex items-center justify-center gap-1 px-3 py-2 border border-slate-200 dark:border-slate-700 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-700/50 text-xs font-bold text-slate-600 dark:text-slate-400 disabled:opacity-40 transition-all cursor-pointer"
                    >
                      <ChevronLeft className="w-4 h-4" />
                      <span>Anterior</span>
                    </button>
                    <span className="text-xs font-bold text-slate-500 dark:text-slate-400 tracking-wider">
                      Página {transfersPage}
                    </span>
                    <button
                      onClick={() => setTransfersPage(prev => prev + 1)}
                      disabled={!transfersHasNext || isLoadingTransfers}
                      className="flex items-center justify-center gap-1 px-3 py-2 border border-slate-200 dark:border-slate-700 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-700/50 text-xs font-bold text-slate-600 dark:text-slate-400 disabled:opacity-40 transition-all cursor-pointer"
                    >
                      <span>Próxima</span>
                      <ChevronRight className="w-4 h-4" />
                    </button>
                  </div>
                </>
              )}
            </div>
          )}
        </div>
      </div>

      {/* ==========================================
          MODAL DE FORMULÁRIO UNIFICADO (CREATE/EDIT)
          ========================================== */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center px-4">
          {/* Backdrop */}
          <div 
            className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm transition-opacity"
            onClick={() => !isSubmitting && setIsModalOpen(false)}
          />
          
          {/* Caixa do Modal */}
          <div className="relative bg-white dark:bg-slate-800 rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-slate-100 dark:border-slate-700 animate-in fade-in zoom-in-95 duration-150 flex flex-col max-h-[90vh]">
            
            {/* Cabeçalho */}
            <div className="flex items-center justify-between mb-4 shrink-0">
              <h3 className="font-extrabold text-lg text-slate-900 dark:text-white">
                {modalMode === 'create' ? 'Novo Registro Financeiro' : 'Editar Registro'}
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

            {/* Abas Internas do Modal (Transação vs. Transferência) */}
            <div className="flex bg-slate-50 dark:bg-slate-900 p-1 rounded-xl border border-slate-200 dark:border-slate-700 mb-6 shrink-0">
              <button
                type="button"
                disabled={modalMode === 'edit' || isSubmitting}
                onClick={() => setModalTab('transaction')}
                className={`flex-1 py-2 text-xs font-bold rounded-lg transition-all ${
                  modalTab === 'transaction'
                    ? 'bg-white dark:bg-slate-800 text-slate-900 dark:text-white shadow-sm'
                    : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white disabled:opacity-55'
                }`}
              >
                Transação (Receita/Despesa)
              </button>
              <button
                type="button"
                disabled={modalMode === 'edit' || isSubmitting}
                onClick={() => setModalTab('transfer')}
                className={`flex-1 py-2 text-xs font-bold rounded-lg transition-all ${
                  modalTab === 'transfer'
                    ? 'bg-white dark:bg-slate-800 text-slate-900 dark:text-white shadow-sm'
                    : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white disabled:opacity-55'
                }`}
              >
                Transferência de Saldo
              </button>
            </div>

            {/* Formulário Dinâmico com Scroll Interno para telas pequenas */}
            <form onSubmit={handleFormSubmit} className="space-y-4 overflow-y-auto flex-1 pr-1">
              
              {/* Se for TRANSAÇÃO */}
              {modalTab === 'transaction' && (
                <>
                  {/* Seletor Tipo (Receita / Despesa) */}
                  <div>
                    <span className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                      Tipo de Fluxo
                    </span>
                    <div className="grid grid-cols-2 gap-3 p-1 bg-slate-50 dark:bg-slate-900 rounded-xl border border-slate-200 dark:border-slate-700">
                      <button
                        type="button"
                        disabled={isSubmitting}
                        onClick={() => setFormType('Expense')}
                        className={`py-2 text-xs font-bold rounded-lg transition-all ${
                          formType === 'Expense'
                            ? 'bg-rose-500 text-white shadow-sm'
                            : 'text-slate-500 dark:text-slate-400 hover:text-rose-600 disabled:opacity-50'
                        }`}
                      >
                        Despesa
                      </button>
                      <button
                        type="button"
                        disabled={isSubmitting}
                        onClick={() => setFormType('Income')}
                        className={`py-2 text-xs font-bold rounded-lg transition-all ${
                          formType === 'Income'
                            ? 'bg-emerald-500 text-white shadow-sm'
                            : 'text-slate-500 dark:text-slate-400 hover:text-emerald-600 disabled:opacity-50'
                        }`}
                      >
                        Receita
                      </button>
                    </div>
                  </div>

                  {/* Conta Financeira */}
                  <div>
                    <label htmlFor="modal-account" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                      Conta
                    </label>
                    <select
                      id="modal-account"
                      required
                      disabled={isSubmitting}
                      value={formAccountId}
                      onChange={(e) => setFormAccountId(e.target.value)}
                      className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all disabled:opacity-50"
                    >
                      <option value="" disabled>Selecione uma conta...</option>
                      {accounts.map(acc => (
                        <option key={acc.id} value={acc.id}>{acc.name}</option>
                      ))}
                    </select>
                  </div>

                  {/* Categoria Filtrada */}
                  <div>
                    <label htmlFor="modal-category" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                      Categoria
                    </label>
                    <select
                      id="modal-category"
                      required
                      disabled={isSubmitting}
                      value={formCategoryId}
                      onChange={(e) => setFormCategoryId(e.target.value)}
                      className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all disabled:opacity-50"
                    >
                      <option value="" disabled>Selecione uma categoria...</option>
                      {formFilteredCategories.map(cat => (
                        <option key={cat.id} value={cat.id}>{cat.name}</option>
                      ))}
                    </select>
                  </div>
                </>
              )}

              {/* Se for TRANSFERÊNCIA */}
              {modalTab === 'transfer' && (
                <>
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    {/* Conta de Origem */}
                    <div>
                      <label htmlFor="modal-origin-account" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                        Conta de Origem
                      </label>
                      <select
                        id="modal-origin-account"
                        required
                        disabled={isSubmitting}
                        value={formOriginAccountId}
                        onChange={(e) => setFormOriginAccountId(e.target.value)}
                        className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all disabled:opacity-50"
                      >
                        <option value="" disabled>Selecione origem...</option>
                        {accounts.map(acc => (
                          <option key={acc.id} value={acc.id}>{acc.name}</option>
                        ))}
                      </select>
                    </div>

                    {/* Conta de Destino */}
                    <div>
                      <label htmlFor="modal-destination-account" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                        Conta de Destino
                      </label>
                      <select
                        id="modal-destination-account"
                        required
                        disabled={isSubmitting}
                        value={formDestinationAccountId}
                        onChange={(e) => setFormDestinationAccountId(e.target.value)}
                        className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all disabled:opacity-50"
                      >
                        <option value="" disabled>Selecione destino...</option>
                        {accounts.map(acc => (
                          <option key={acc.id} value={acc.id}>{acc.name}</option>
                        ))}
                      </select>
                    </div>
                  </div>

                  {/* Impedimento Visual Reativo para Contas Iguais */}
                  {formOriginAccountId && formDestinationAccountId && formOriginAccountId === formDestinationAccountId && (
                    <div className="flex items-center gap-2 text-rose-600 dark:text-rose-400 bg-rose-50 dark:bg-rose-950/20 px-4 py-3 rounded-xl border border-rose-100 dark:border-rose-900/35 text-xs font-bold animate-pulse">
                      <AlertCircle className="w-5 h-5 shrink-0" />
                      <span>Impedimento: A conta de origem não pode ser idêntica à de destino. Escolha contas distintas para enviar.</span>
                    </div>
                  )}
                </>
              )}

              {/* Campos Comuns (Descrição, Valor e Data) */}
              <div>
                <label htmlFor="modal-description" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Descrição
                </label>
                <input
                  id="modal-description"
                  type="text"
                  required
                  disabled={isSubmitting}
                  value={formDescription}
                  onChange={(e) => setFormDescription(e.target.value)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all disabled:opacity-50"
                  placeholder="Ex: Assinatura, Salário, Transferência mensal..."
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                {/* Valor */}
                <div>
                  <label htmlFor="modal-amount" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                    Valor (R$)
                  </label>
                  <input
                    id="modal-amount"
                    type="number"
                    step="0.01"
                    min="0.01"
                    required
                    disabled={isSubmitting}
                    value={formAmount}
                    onChange={(e) => setFormAmount(e.target.value)}
                    className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all disabled:opacity-50"
                    placeholder="0.00"
                  />
                </div>

                {/* Data */}
                <div>
                  <label htmlFor="modal-date" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                    Data de Movimentação
                  </label>
                  <input
                    id="modal-date"
                    type="date"
                    required
                    disabled={isSubmitting}
                    value={formDate}
                    onChange={(e) => setFormDate(e.target.value)}
                    className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 text-sm transition-all disabled:opacity-50"
                  />
                </div>
              </div>

              {/* Ações / Botões */}
              <div className="pt-4 flex gap-3 border-t border-slate-100 dark:border-slate-700/50 mt-6 shrink-0">
                <button
                  type="button"
                  disabled={isSubmitting}
                  onClick={() => setIsModalOpen(false)}
                  className="flex-1 py-3 px-4 border border-slate-200 dark:border-slate-700 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-700/50 text-sm font-semibold text-slate-700 dark:text-slate-200 transition-all h-11 disabled:opacity-50 cursor-pointer text-center"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting || (modalTab === 'transfer' && formOriginAccountId === formDestinationAccountId)}
                  className="flex-1 py-3 px-4 rounded-xl shadow-lg shadow-indigo-600/10 hover:shadow-indigo-600/20 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-500 disabled:bg-slate-300 dark:disabled:bg-slate-700 disabled:text-slate-500 dark:disabled:text-slate-500 disabled:cursor-not-allowed transition-all h-11 flex items-center justify-center gap-2 cursor-pointer"
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
