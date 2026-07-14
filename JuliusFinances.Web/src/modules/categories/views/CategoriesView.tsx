import { useState, useEffect } from 'react';
import Layout from '@/shared/components/Layout';
import { 
  Tag, 
  Plus, 
  Trash2, 
  Pencil, 
  X, 
  Loader2, 
  AlertCircle,
  TrendingUp,
  TrendingDown,
  ArrowUpRight,
  ArrowDownRight,
  ArrowLeftRight,
  Utensils,
  ShoppingBag,
  Car,
  DollarSign,
  Heart,
  Home,
  GraduationCap,
  Sparkles,
  Wrench,
  Gift
} from 'lucide-react';
import axios from 'axios';
import { apiClient } from '@/core/api/client';

interface Category {
  id: string;
  name: string;
  flowType: 'Income' | 'Expense' | 'Both';
  isGlobal: boolean;
}

// Mapeamento visual dinâmico puro front-end baseado no nome e tipo de fluxo
function getCategoryVisuals(name: string, flowType: 'Income' | 'Expense' | 'Both') {
  // Normaliza o nome para remover acentos e converter para minúsculas
  const normalized = name
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');

  let IconComponent = Tag;

  // Dicionário de Palavras-Chave (UX Premium)
  if (/alimentac|restaurant|comida|lanche|gourmet|pizza|massa|burg/i.test(normalized)) {
    IconComponent = Utensils;
  } else if (/mercado|supermercado|feira|compra|shopp|loja|vestuar/i.test(normalized)) {
    IconComponent = ShoppingBag;
  } else if (/transport|carro|combustiv|uber|taxi|veicul|viagem|metro|onibus/i.test(normalized)) {
    IconComponent = Car;
  } else if (/salario|pagament|receit|renda|provent|faturam|freelance/i.test(normalized)) {
    IconComponent = DollarSign;
  } else if (/saude|medic|farmac|hospital|clinic|dentist|psicolog/i.test(normalized)) {
    IconComponent = Heart;
  } else if (/casa|aluguel|habitac|moradi|luz|agua|internet|gas|condominio/i.test(normalized)) {
    IconComponent = Home;
  } else if (/educac|escol|curs|faculdad|livro|estud|colegio/i.test(normalized)) {
    IconComponent = GraduationCap;
  } else if (/lazer|entreten|cinema|show|festa|pub|viagem|feriad|streaming|netflix|spotify/i.test(normalized)) {
    IconComponent = Sparkles;
  } else if (/servic|manutenc|reform|ajust|consert|oficin|mecanic/i.test(normalized)) {
    IconComponent = Wrench;
  } else if (/present|doac|mimo|solidari|brinde/i.test(normalized)) {
    IconComponent = Gift;
  } else {
    // Fallbacks dinâmicos por FlowType
    if (flowType === 'Income') IconComponent = ArrowUpRight;
    else if (flowType === 'Expense') IconComponent = ArrowDownRight;
    else IconComponent = ArrowLeftRight;
  }

  // Estilos de Destaque e Badges baseados no FlowType
  let styles = {
    bg: 'bg-slate-50 dark:bg-slate-950/40 text-slate-600 dark:text-slate-400',
    border: 'border-slate-100 dark:border-slate-900/30',
    hover: 'hover:border-slate-200 dark:hover:border-slate-800',
    badgeBg: 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400',
    labelText: 'Duplo/Sistema'
  };

  if (flowType === 'Income') {
    styles = {
      bg: 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400',
      border: 'border-emerald-100 dark:border-emerald-900/30',
      hover: 'hover:border-emerald-200 dark:hover:border-emerald-800',
      badgeBg: 'bg-emerald-100 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300',
      labelText: 'Receita'
    };
  } else if (flowType === 'Expense') {
    styles = {
      bg: 'bg-rose-50 dark:bg-rose-950/40 text-rose-600 dark:text-rose-400',
      border: 'border-rose-100 dark:border-rose-900/30',
      hover: 'hover:border-rose-200 dark:hover:border-rose-800',
      badgeBg: 'bg-rose-100 dark:bg-rose-950/50 text-rose-700 dark:text-rose-300',
      labelText: 'Despesa'
    };
  }

  return { IconComponent, styles };
}

export default function CategoriesView() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // Controle de Abas / Filtro
  const [activeTab, setActiveTab] = useState<'All' | 'Income' | 'Expense' | 'Both'>('All');

  // Estados do Modal
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [modalMode, setModalMode] = useState<'create' | 'edit'>('create');
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Campos do Formulário
  const [name, setName] = useState('');
  const [flowType, setFlowType] = useState<'Income' | 'Expense' | 'Both'>('Expense');

  const fetchCategories = async (signal?: AbortSignal) => {
    try {
      setIsLoading(true);
      setError('');
      const response = await apiClient.get<Category[]>('/categories', { signal });
      setCategories(response.data);
    } catch (err: any) {
      if (axios.isCancel(err)) {
        return;
      }
      console.error('Erro ao buscar categorias:', err);
      setError('Não foi possível carregar as categorias. Por favor, verifique sua conexão.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    const controller = new AbortController();
    fetchCategories(controller.signal);

    return () => {
      controller.abort();
    };
  }, []);

  const openCreateModal = () => {
    setModalMode('create');
    setEditingCategoryId(null);
    setName('');
    setFlowType('Expense');
    setIsModalOpen(true);
  };

  const openEditModal = (category: Category) => {
    setModalMode('edit');
    setEditingCategoryId(category.id);
    setName(category.name);
    setFlowType(category.flowType);
    setIsModalOpen(true);
  };

  const handleFormSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const normalizedName = name.trim().replace(/\s+/g, ' ');

    if (!normalizedName || normalizedName.length < 3) {
      alert('O nome da categoria deve conter no mínimo 3 caracteres.');
      return;
    }

    if (normalizedName.length > 100) {
      alert('O nome da categoria deve conter no máximo 100 caracteres.');
      return;
    }

    try {
      setIsSubmitting(true);

      const requestData = {
        name: normalizedName,
        flowType
      };

      if (modalMode === 'create') {
        await apiClient.post('/categories', requestData);
      } else {
        await apiClient.put(`/categories/${editingCategoryId}`, requestData);
      }

      setIsModalOpen(false);
      fetchCategories();
    } catch (err: any) {
      console.error('Erro ao salvar categoria:', err);
      if (err.response?.status === 409) {
        alert('Já existe uma categoria ativa cadastrada com este nome no seu escopo pessoal ou global. Por favor, escolha outro nome.');
      } else {
        const errorMessage = err.response?.data?.detail || err.response?.data?.message || 'Falha ao salvar a categoria.';
        alert(errorMessage);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteCategory = async (id: string, categoryName: string) => {
    const confirmationText = `Deseja realmente excluir a categoria "${categoryName}"?`;
    if (!confirm(confirmationText)) {
      return;
    }

    try {
      await apiClient.delete(`/categories/${id}`);
      fetchCategories();
    } catch (err: any) {
      console.error('Erro ao excluir categoria:', err);
      if (err.response?.status === 400) {
        alert('Atenção: Esta categoria não pode ser excluída porque possui transações financeiras vinculadas no seu histórico. Para poder excluí-la, você deve primeiro alterar a categoria ou excluir as transações correspondentes.');
      } else {
        const errorMessage = err.response?.data?.detail || err.response?.data?.message || 'Não foi possível excluir a categoria.';
        alert(errorMessage);
      }
    }
  };

  // Filtragem local baseada na aba ativa
  const filteredCategories = categories.filter((c) => {
    if (activeTab === 'All') return true;
    return c.flowType === activeTab;
  });

  if (isLoading && categories.length === 0) {
    return (
      <Layout>
        <div className="flex flex-col items-center justify-center min-h-[60vh] gap-4">
          <Loader2 className="w-12 h-12 text-indigo-600 animate-spin" />
          <p className="text-sm text-slate-500 dark:text-slate-400 font-semibold tracking-wide">
            Carregando categorias financeiras...
          </p>
        </div>
      </Layout>
    );
  }

  if (error && categories.length === 0) {
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
            onClick={() => fetchCategories()}
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
            Categorias
          </h2>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium mt-1">
            Organize e classifique seus lançamentos por tipo de fluxo financeiro.
          </p>
        </div>
        <button
          onClick={openCreateModal}
          className="flex items-center justify-center gap-2 bg-indigo-600 hover:bg-indigo-500 text-white px-5 py-3 rounded-xl font-semibold shadow-lg shadow-indigo-600/10 hover:shadow-indigo-600/20 transition-all self-start"
        >
          <Plus className="w-5 h-5" />
          <span>Nova Categoria</span>
        </button>
      </div>

      {/* Abas de Filtros */}
      <div className="flex overflow-x-auto bg-slate-100 dark:bg-slate-900 p-1 rounded-xl border border-slate-200 dark:border-slate-700/50 w-full mb-6 gap-1 max-w-lg scrollbar-none">
        <button
          onClick={() => setActiveTab('All')}
          className={`flex-1 min-w-[70px] px-3 py-2 text-xs font-bold rounded-lg transition-all ${
            activeTab === 'All'
              ? 'bg-white dark:bg-slate-800 text-slate-900 dark:text-white shadow-sm'
              : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
          }`}
        >
          Todas
        </button>
        <button
          onClick={() => setActiveTab('Income')}
          className={`flex-1 min-w-[90px] px-3 py-2 text-xs font-bold rounded-lg transition-all flex items-center justify-center gap-1.5 ${
            activeTab === 'Income'
              ? 'bg-emerald-500 text-white shadow-sm'
              : 'text-slate-500 dark:text-slate-400 hover:text-emerald-600 dark:hover:text-emerald-400'
          }`}
        >
          <TrendingUp className="w-3.5 h-3.5" />
          <span>Receitas</span>
        </button>
        <button
          onClick={() => setActiveTab('Expense')}
          className={`flex-1 min-w-[90px] px-3 py-2 text-xs font-bold rounded-lg transition-all flex items-center justify-center gap-1.5 ${
            activeTab === 'Expense'
              ? 'bg-rose-500 text-white shadow-sm'
              : 'text-slate-500 dark:text-slate-400 hover:text-rose-600 dark:hover:text-rose-400'
          }`}
        >
          <TrendingDown className="w-3.5 h-3.5" />
          <span>Despesas</span>
        </button>
        <button
          onClick={() => setActiveTab('Both')}
          className={`flex-1 min-w-[110px] px-3 py-2 text-xs font-bold rounded-lg transition-all flex items-center justify-center gap-1.5 ${
            activeTab === 'Both'
              ? 'bg-slate-500 dark:bg-slate-600 text-white shadow-sm'
              : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
          }`}
        >
          <ArrowLeftRight className="w-3.5 h-3.5" />
          <span>Sistema</span>
        </button>
      </div>

      {/* Grid de Categorias */}
      {filteredCategories.length === 0 ? (
        <div className="bg-white dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700/50 p-12 text-center shadow-sm max-w-xl mx-auto mt-6">
          <div className="w-14 h-14 bg-indigo-50 dark:bg-indigo-950/40 rounded-2xl flex items-center justify-center text-indigo-600 dark:text-indigo-400 mx-auto mb-4">
            <Tag className="w-7 h-7" />
          </div>
          <h3 className="text-lg font-bold text-slate-900 dark:text-white mb-2">Nenhuma categoria encontrada</h3>
          <p className="text-sm text-slate-500 dark:text-slate-400 font-medium leading-relaxed mb-6">
            {activeTab === 'All' 
              ? 'Crie sua primeira categoria personalizada para detalhar ainda mais seus gastos!' 
              : 'Não há nenhuma categoria cadastrada correspondente ao filtro de aba ativo.'}
          </p>
          {activeTab === 'All' && (
            <button
              onClick={openCreateModal}
              className="inline-flex items-center gap-2 bg-indigo-600 hover:bg-indigo-500 text-white px-5 py-2.5 rounded-xl font-semibold text-sm transition-all"
            >
              <Plus className="w-4 h-4" />
              <span>Cadastrar Primeira Categoria</span>
            </button>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredCategories.map((category) => {
            const { IconComponent, styles } = getCategoryVisuals(category.name, category.flowType);

            return (
              <div 
                key={category.id} 
                className={`bg-white dark:bg-slate-800 p-6 rounded-2xl border ${styles.border} ${styles.hover} shadow-sm transition-all relative overflow-hidden flex flex-col justify-between h-40`}
              >
                {/* Top Section */}
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className={`w-12 h-12 rounded-xl flex items-center justify-center shadow-inner shrink-0 ${styles.bg}`}>
                      <IconComponent className="w-6 h-6" />
                    </div>
                    <div className="min-w-0">
                      <h4 className="font-extrabold text-slate-900 dark:text-white truncate text-base leading-snug">
                        {category.name}
                      </h4>
                      <div className="flex items-center gap-1.5 mt-1 flex-wrap">
                        <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full uppercase tracking-wider ${styles.badgeBg}`}>
                          {styles.labelText}
                        </span>
                        {category.isGlobal && (
                          <span className="text-[10px] font-bold px-2 py-0.5 rounded-full uppercase tracking-wider bg-slate-100 dark:bg-slate-700 text-slate-500 dark:text-slate-300">
                            Global
                          </span>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Actions */}
                  {!category.isGlobal && (
                    <div className="flex items-center gap-1 shrink-0">
                      <button
                        onClick={() => openEditModal(category)}
                        className="text-slate-400 hover:text-indigo-600 dark:hover:text-indigo-400 p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-700 transition-colors"
                        title="Editar Categoria"
                      >
                        <Pencil className="w-4 h-4" />
                      </button>
                      <button
                        onClick={() => handleDeleteCategory(category.id, category.name)}
                        className="text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 p-2 rounded-lg hover:bg-rose-50 dark:hover:bg-rose-950/20 transition-colors"
                        title="Excluir Categoria"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  )}
                </div>

                {/* Bottom decorative/interactive state */}
                <div className="text-[11px] text-slate-400 dark:text-slate-500 font-semibold tracking-wider uppercase mt-4">
                  {category.isGlobal ? 'Somente Leitura' : 'Propriedade Pessoal'}
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
                {modalMode === 'create' ? 'Nova Categoria Financeira' : 'Editar Categoria Financeira'}
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
                <label htmlFor="category-name" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Nome da Categoria
                </label>
                <input
                  id="category-name"
                  type="text"
                  required
                  disabled={isSubmitting}
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all disabled:opacity-50"
                  placeholder="Ex: Alimentação, Combustível, Salário, Internet..."
                />
              </div>

              {/* Tipo de Fluxo (FlowType) */}
              <div>
                <label htmlFor="category-flow-type" className="block text-sm font-semibold text-slate-700 dark:text-slate-200 mb-1.5">
                  Tipo de Fluxo Financeiro
                </label>
                <select
                  id="category-flow-type"
                  disabled={isSubmitting}
                  value={flowType}
                  onChange={(e) => setFlowType(e.target.value as any)}
                  className="block w-full px-3 py-2.5 border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-900 text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 text-sm transition-all disabled:opacity-50"
                >
                  <option value="Expense">Despesa (Saída)</option>
                  <option value="Income">Receita (Entrada)</option>
                  <option value="Both">Ambos / Ajuste do Sistema</option>
                </select>
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