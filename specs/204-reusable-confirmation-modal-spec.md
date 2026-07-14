# Especificação técnica: Modal de Confirmação Reutilizável (useConfirm)

Este documento estabelece as especificações, contratos de código, guia visual e padrões de UX para o componente global de Modal de Confirmação (`ConfirmationModal`) e seu respectivo hook assíncrono (`useConfirm`). 

Todos os novos fluxos de desenvolvimento do frontend que exigem confirmação do usuário (como exclusões, arquivamentos, edições destrutivas, saídas desautorizadas de formulários, etc.) devem obrigatoriamente fazer uso deste mecanismo, sendo estritamente proibido o uso de chamadas nativas e síncronas do JavaScript, como `confirm()`.

---

## 1. Motivação e Objetivos

* **UX Coesa:** Unificar o design de caixas de diálogo no projeto, respeitando a paleta de cores, tipografia, bordas arredondadas e o modo escuro (Dark/Light Mode).
* **DX Simplificada (Zero Boilerplate):** Utilizar o poder de Promises e React Context para invocar a modal de maneira assíncrona por meio de um único hook customizado, sem a necessidade de instanciar estados locais de abertura/fechamento ou declarar o componente manualmente em cada view.
* **Acessibilidade:** Suporte a teclas físicas (como fechar via `Escape`) e acessibilidade via teclado nativo do navegador para o foco nos botões de ação de forma ágil.
* **Responsividade:** Layouts fluídos com foco mobile-first (adaptabilidade na barra inferior ou centrada de forma harmônica em diferentes tamanhos de viewport).

---

## 2. Estrutura e Localização dos Arquivos

O mecanismo de confirmação está modularizado e distribuído dentro da pasta compartilhada `src/shared/`:

```text
src/shared/
├── components/
│   └── ConfirmationModal.tsx     # O componente visual de UI e listeners internos
└── context/
    └── ConfirmContext.tsx        # O Context Provider de estado assíncrono e Custom Hook
```

---

## 3. Contratos de Tipo e Tipagens (TypeScript)

### 3.1. `ConfirmOptions`
Determina a assinatura de personalização aceita pelo hook `useConfirm`:

```typescript
export type ConfirmType = 'info' | 'warning' | 'danger' | 'success';

export interface ConfirmOptions {
  title: string;              // Título principal exibido na modal
  message: string;            // Texto de apoio explicativo (suporta \n para quebras de linha)
  type?: ConfirmType;         // Estilo temático do diálogo (padrão: 'info')
  confirmText?: string;       // Rótulo para o botão de ação principal (padrão: 'Confirmar')
  cancelText?: string;        // Rótulo para o botão de cancelamento (padrão: 'Cancelar')
  isBlocking?: boolean;       // Define se a modal é bloqueante (padrão: false)
}
```

---

## 4. Comportamento: Bloqueante vs Não-Bloqueante

A modal de confirmação suporta dois comportamentos distintos com base na propriedade `isBlocking`:

| Característica | Bloqueante (`isBlocking: true`) | Não-Bloqueante (`isBlocking: false`) |
| :--- | :--- | :--- |
| **Pressione `Escape`** | **Não fecha** a modal | **Fecha e resolve** com `false` |
| **Clique fora (Backdrop)** | **Não fecha** a modal | **Fecha e resolve** com `false` |
| **Botão de Fechar (`X`)** | **Oculto** na interface | **Visível** no canto superior direito |
| **Foco Padrão** | Foca automaticamente no botão principal | Foca automaticamente no botão principal |

---

## 5. Mapeamento Visual e Temas

Cada tipo (`ConfirmType`) possui uma paleta de cores e ícones dedicados mapeados no Tailwind CSS v4 para manter perfeita concordância visual com o nível de gravidade da ação:

### 5.1. `danger` (Perigo / Exclusões Destrutivas)
* **Ícone:** `AlertCircle`
* **Classe do Ícone:** `text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-950/40`
* **Botão Confirmar:** `bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600 focus:ring-red-500/30`

### 5.2. `warning` (Aviso / Arquivamento / Impactos Reversíveis)
* **Ícone:** `AlertTriangle`
* **Classe do Ícone:** `text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-950/40`
* **Botão Confirmar:** `bg-amber-500 hover:bg-amber-600 dark:bg-amber-600 dark:hover:bg-amber-700 focus:ring-amber-500/30`

### 5.3. `success` (Sucesso / Aprovações)
* **Ícone:** `CheckCircle2`
* **Classe do Ícone:** `text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/40`
* **Botão Confirmar:** `bg-emerald-600 hover:bg-emerald-700 dark:bg-emerald-500 dark:hover:bg-emerald-600 focus:ring-emerald-500/30`

### 5.4. `info` (Informativo / Confirmações Gerais)
* **Ícone:** `Info`
* **Classe do Ícone:** `text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-950/40`
* **Botão Confirmar:** `bg-indigo-600 hover:bg-indigo-700 dark:bg-indigo-500 dark:hover:bg-indigo-600 focus:ring-indigo-500/30`

---

## 6. Guia de Uso

### 6.1. Configuração Global (`App.tsx`)
O `ConfirmProvider` deve envolver a aplicação para renderizar o componente dinamicamente:

```tsx
import { ConfirmProvider } from '@/shared/context/ConfirmContext';

export default function App() {
  return (
    <ThemeProvider>
      <ConfirmProvider>
        {/* Rotas e restante da aplicação */}
      </ConfirmProvider>
    </ThemeProvider>
  );
}
```

### 6.2. Uso nos Componentes (`useConfirm`)
Para invocar o diálogo em qualquer local do código, utilize o hook de maneira assíncrona:

```tsx
import { useConfirm } from '@/shared/context/ConfirmContext';

export default function ExemploView() {
  const confirm = useConfirm();

  const handleDelete = async (id: string) => {
    // Retorna uma promessa que resolve em true (confirmar) ou false (cancelar)
    const confirmed = await confirm({
      title: 'Excluir Item Crítico',
      message: 'Tem certeza de que deseja realizar esta operação? Esta ação não pode ser desfeita.',
      type: 'danger',
      confirmText: 'Excluir permanentemente',
      isBlocking: true, // Força interação explícita nos botões
    });

    if (confirmed) {
      // Prossiga com a ação de exclusão
    }
  };

  return <button onClick={() => handleDelete('1')}>Excluir</button>;
}
```
