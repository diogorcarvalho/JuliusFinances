import { useEffect, useRef } from 'react';
import { AlertCircle, AlertTriangle, Info, CheckCircle2, X } from 'lucide-react';
import { cn } from '@/shared/utils/cn';

export type ConfirmType = 'info' | 'warning' | 'danger' | 'success';

interface ConfirmationModalProps {
  isOpen: boolean;
  title: string;
  message: string;
  type: ConfirmType;
  confirmText?: string;
  cancelText?: string;
  isBlocking: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export default function ConfirmationModal({
  isOpen,
  title,
  message,
  type,
  confirmText = 'Confirmar',
  cancelText = 'Cancelar',
  isBlocking,
  onConfirm,
  onCancel,
}: ConfirmationModalProps) {
  const modalRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isBlocking) {
        onCancel();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    
    // Focar no botão de confirmação quando abrir
    const focusTimer = setTimeout(() => {
      const confirmBtn = modalRef.current?.querySelector('[data-confirm-btn]') as HTMLButtonElement | null;
      confirmBtn?.focus();
    }, 50);

    return () => {
      window.removeEventListener('keydown', handleKeyDown);
      clearTimeout(focusTimer);
    };
  }, [isOpen, isBlocking, onCancel]);

  if (!isOpen) return null;

  // Configuração visual específica de acordo com o tema/tipo
  const config = {
    danger: {
      icon: AlertCircle,
      iconColorClass: 'text-red-600 dark:text-red-400',
      iconBgClass: 'bg-red-50 dark:bg-red-950/40',
      confirmButtonClass: 'bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600 focus:ring-red-500/30 text-white',
    },
    warning: {
      icon: AlertTriangle,
      iconColorClass: 'text-amber-600 dark:text-amber-400',
      iconBgClass: 'bg-amber-50 dark:bg-amber-950/40',
      confirmButtonClass: 'bg-amber-500 hover:bg-amber-600 dark:bg-amber-600 dark:hover:bg-amber-700 focus:ring-amber-500/30 text-white',
    },
    success: {
      icon: CheckCircle2,
      iconColorClass: 'text-emerald-600 dark:text-emerald-400',
      iconBgClass: 'bg-emerald-50 dark:bg-emerald-950/40',
      confirmButtonClass: 'bg-emerald-600 hover:bg-emerald-700 dark:bg-emerald-500 dark:hover:bg-emerald-600 focus:ring-emerald-500/30 text-white',
    },
    info: {
      icon: Info,
      iconColorClass: 'text-indigo-600 dark:text-indigo-400',
      iconBgClass: 'bg-indigo-50 dark:bg-indigo-950/40',
      confirmButtonClass: 'bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:ring-indigo-500/30 text-white',
    },
  }[type];

  const IconComponent = config.icon;

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget && !isBlocking) {
      onCancel();
    }
  };

  return (
    <div
      onClick={handleBackdropClick}
      className="fixed inset-0 bg-slate-900/60 dark:bg-slate-950/80 backdrop-blur-sm z-50 flex items-center justify-center p-4 transition-all duration-300"
    >
      <div
        ref={modalRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-modal-title"
        className="bg-white dark:bg-slate-800 rounded-2xl max-w-md w-full shadow-2xl border border-slate-100 dark:border-slate-700/50 overflow-hidden transform transition-all flex flex-col animate-in fade-in zoom-in-95 duration-200"
      >
        {/* Header com botão de fechar (Apenas se não for bloqueante) */}
        {!isBlocking && (
          <div className="flex justify-end p-2 absolute top-2 right-2 z-10">
            <button
              onClick={onCancel}
              className="text-slate-400 hover:text-slate-600 dark:text-slate-500 dark:hover:text-slate-300 p-1.5 rounded-full hover:bg-slate-100 dark:hover:bg-slate-700/50 transition-all focus:outline-none focus:ring-2 focus:ring-indigo-500/20"
              aria-label="Fechar"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        )}

        {/* Conteúdo Principal */}
        <div className="p-6 pt-8 flex flex-col items-center text-center sm:items-start sm:text-left sm:flex-row gap-4">
          <div className={cn("w-12 h-12 rounded-full flex items-center justify-center shrink-0 shadow-sm", config.iconBgClass)}>
            <IconComponent className={cn("w-6 h-6", config.iconColorClass)} />
          </div>
          <div className="space-y-2 flex-1">
            <h2
              id="confirm-modal-title"
              className="text-lg font-bold text-slate-900 dark:text-white leading-6 tracking-tight"
            >
              {title}
            </h2>
            <p className="text-sm text-slate-500 dark:text-slate-400 leading-relaxed whitespace-pre-line font-medium">
              {message}
            </p>
          </div>
        </div>

        {/* Rodapé com Ações */}
        <div className="flex flex-col-reverse sm:flex-row justify-end gap-3 p-6 bg-slate-50 dark:bg-slate-900/40 border-t border-slate-100 dark:border-slate-700/30">
          <button
            onClick={onCancel}
            className="w-full sm:w-auto px-5 py-2.5 rounded-xl text-sm font-semibold border border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300 transition-all focus:outline-none focus:ring-2 focus:ring-slate-500/20"
          >
            {cancelText}
          </button>
          <button
            data-confirm-btn
            onClick={onConfirm}
            className={cn(
              "w-full sm:w-auto px-5 py-2.5 rounded-xl text-sm font-semibold transition-all shadow-sm focus:outline-none focus:ring-4 active:scale-[0.98]",
              config.confirmButtonClass
            )}
          >
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  );
}
