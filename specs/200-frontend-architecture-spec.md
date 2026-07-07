# Especificação de Arquitetura Frontend Global

Este documento estabelece as diretrizes arquiteturais, padrões de design, fluxo de autenticação e práticas de desenvolvimento para a aplicação Web (SPA) do JuliusFinances. Todas as futuras especificações de telas e fluxos visuais devem seguir estas definições para garantir performance, responsividade e consistência de código.

---

## 1. Stack Tecnológica & Estrutura do Projeto

A aplicação adota um ecossistema focado em alta performance de desenvolvimento, build otimizado e padrões modernos do ecossistema JavaScript/TypeScript.

### 1.1. Tecnologias Centrais

* **Vite:** Ferramenta de build e servidor de desenvolvimento ultra-rápido utilizando ESM (ECMAScript Modules) nativos.
* **React.js (Moderno):** Uso exclusivo de componentes funcionais, gerenciamento de estado baseado em Hooks (`useState`, `useEffect`, `useContext`) e a nova arquitetura concorrente do ecossistema.
* **TypeScript:** Tipagem estática obrigatória para contratos de API, payloads, rotas e propriedades de componentes, garantindo previsibilidade idêntica à do backend.
* **shadcn/ui:** Arquitetura de componentes de interface baseada em primitivos acessíveis (Radix UI) e totalmente controlados pelo Tailwind CSS. Os componentes são gerados diretamente para o diretório local de código do projeto, permitindo customização total sem dependências engessadas de pacotes externos.

### 1.2. Localização & Estrutura de Pastas (JuliusFinances.Web)

O projeto frontend está localizado no diretório raiz do repositório sob a pasta **`JuliusFinances.Web/`**. A organização de pastas segue uma arquitetura modular por features e com compartilhamento centralizado de componentes de UI e lógica core:

```text
JuliusFinances.Web/
├── public/                 # Assets públicos estáticos (favicon, etc.)
├── src/
│   ├── assets/             # Imagens, vetores (SVG) e mídias do app
│   ├── core/               # Lógica global e contratos da aplicação
│   │   ├── api/            # Configuração do cliente HTTP e interceptores
│   │   ├── guards/         # Guardas de rotas (públicas e privadas)
│   │   └── types/          # Definições globais de tipos e interfaces do TypeScript
│   ├── modules/            # Módulos de domínio / features da aplicação
│   │   ├── auth/           # Login, registro, recuperação de conta
│   │   ├── dashboard/      # Resumos financeiros, gráficos e saldos
│   │   └── transactions/   # Listagem e criação de receitas/despesas/transferências
│   ├── shared/             # Recursos reutilizáveis compartilhados
│   │   ├── components/     # Componentes de UI genéricos e do shadcn/ui (ui/)
│   │   ├── hooks/          # Hooks customizados reutilizáveis (useTheme, etc.)
│   │   └── utils/          # Funções utilitárias (formatadores, validadores)
│   ├── App.tsx             # Componente raiz de roteamento e provedores globais
│   └── main.tsx            # Ponto de entrada da renderização React
├── .env.development        # Configurações para ambiente de desenvolvimento local
├── .env.production         # Configurações para build de produção
├── tailwind.config.js      # Configurações do Tailwind CSS
├── tsconfig.json           # Configurações de tipagem e compilação do TypeScript
└── vite.config.ts          # Configurações de build do Vite
```

---

## 2. Estilização, Responsividade & Design System (Tailwind CSS + shadcn/ui)

Para viabilizar uma interface rápida, limpa e altamente customizável sem a sobrecarga de arquivos CSS gigantescos, o projeto adota o **Tailwind CSS** em conjunto com a filosofia headless do **shadcn/ui**.

### 2.1. Abordagem Mobile-First

A estilização deve ser escrita pensando primeiro em dispositivos menores (smartphones) e expandindo progressivamente para telas maiores usando os prefixos de breakpoint nativos do Tailwind (`md:`, `lg:`, `xl:`).

### 2.2. Diretrizes de Responsividade

A interface deve se adaptar perfeitamente a três grandes grupos de telas:

1. **Smarthphone (Mobile):** Visão em coluna única, menus expansíveis ou retráteis estilo hambúrguer ou barra de navegação inferior (bottom navigation bar). Toques fáceis com área mínima de clique de 44 por 44 pixels.
2. **Tablet:** Layouts híbridos, grids de duas colunas para cards de resumo financeiro e tabelas transformadas em listas roláveis ou colunas compactas.
3. **Monitor (Desktop):** Navegação lateral fixa, dashboards multi-colunas aproveitando o espaço horizontal para gráficos avançados e tabelas financeiras completas.

### 2.3. Primitivos de Interface Avançados

Componentes que exigem comportamentos complexos e alta acessibilidade — como Dropdown Menus, Modais (Dialogs), Tabs e Selects com busca textual integrada (Combobox) — serão criados exclusivamente através do ecossistema shadcn/ui, habitando a pasta compartilhada `src/shared/components/ui/`.

### 2.4. Boas Práticas de Componentização & Reutilização

Para garantir que a base de código do frontend permaneça limpa, legível e de fácil manutenção ao longo do tempo, o desenvolvimento deve seguir as seguintes diretrizes de arquitetura de componentes:

1. **Decomposição de Páginas (Princípio de Responsabilidade Única):**
   * **Páginas Orquestradoras:** Os arquivos de visualização de páginas (ex: `DashboardView.tsx`, `TransactionsView.tsx` na pasta `src/modules/`) devem atuar apenas como orquestradores de alto nível. Eles devem conter o mínimo de lógica visual direta ou JSX longo.
   * **Divisão em Componentes Menores:** Sempre que uma página ou componente acumular JSX longo e complexo (ex: ultrapassando 150 a 200 linhas de código), ela deve ser dividida em subcomponentes focados e coesos (ex: `TransactionSummaryCard.tsx`, `RecentTransactionsTable.tsx`, `TransactionFilters.tsx`).
   * **Co-localização de Componentes Locais:** Componentes menores criados para atender exclusivamente a um módulo/tela específico devem ser mantidos dentro da pasta desse módulo (ex: `src/modules/transactions/components/`), evitando poluir a pasta compartilhada global.

2. **Desenvolvimento de Componentes Reutilizáveis (DRY - Don't Repeat Yourself):**
   * **Componentes de UI Compartilhados (Shared UI):** Componentes puramente visuais e comportamentais genéricos (ex: botões personalizados, cards de layout, skeletons de carregamento, inputs padrão) que aparecem em múltiplos módulos devem habitar a pasta compartilhada central `src/shared/components/`.
   * **Propriedades (Props) Claras e Tipadas:** Todos os componentes compartilhados devem possuir uma interface TypeScript estrita para suas `Props`. Eles devem prever callbacks de evento explícitos (ex: `onAction`, `onClose`, `onSuccess`) e aceitar propriedades nativas do HTML (como `className` combinada com o utilitário `cn` do Tailwind) para permitir extensibilidade visual controlada.
   * **Separar Lógica de Apresentação (Componentes Puros vs. Impuros):**
     * **Componentes de Apresentação (Puros):** Devem ser a maioria na pasta compartilhada. Recebem dados via `props` e notificam ações por callbacks, sem acoplamento direto com chamadas de API, roteadores ou estados de negócios. Isso facilita a testabilidade e o reuso em contextos variados.
     * **Componentes Recipientes (Impuros):** Habitantes de `src/modules/`, eles gerenciam estados, disparam chamadas à API, consomem hooks de mutação/dados e conectam-se diretamente com regras de negócio.

---

## 3. Fluxo de Rotas & Proteção de Acesso (Auth Guard)

O roteamento da aplicação será gerenciado pelo **React Router** (ou alternativa moderna baseada em arquivos/configuração), dividindo a aplicação em dois ecossistemas isolados:

### 3.1. Rotas Públicas (Anônimas)

Acessíveis sem qualquer token de autenticação. Se um usuário com um token válido e não expirado tentar acessá-las, deve ser redirecionado automaticamente para o Dashboard interno. Caso o token armazenado esteja expirado ou corrompido, o armazenamento local correspondente deve ser limpo e o acesso à rota pública deve ser permitido normalmente, evitando loops de redirecionamento.

* `GET /login` — Tela de autenticação de usuários.



### 3.2. Rotas Privadas (Protegidas / Restritas)

Qualquer tentativa de acesso direto via URL sem um token válido ou com um token já expirado intercepta a navegação, remove o token inválido do armazenamento e redireciona o cliente para `/login`.

* `GET /` ou `/dashboard` — Visão geral financeira e saldos.
* `GET /transactions` — Listagem paginada e gerenciamento de receitas/despesas.


* `GET /accounts` — Gerenciamento de contas e carteiras.



### 3.3. Mecanismo de Guard e Token JWT

* **Persistência do Token:** Ao realizar o login com sucesso, o token de acesso (`accessToken`) retornado pela API deve ser armazenado no `localStorage` do navegador, simplificando o acesso client-side necessário para as validações de rota e injeções de cabeçalhos.

* **Validação de Expiração (Client-Side):** O Auth Guard deve inspecionar e decodificar o payload do JWT antes de liberar a navegação para rotas privadas. Se o campo de expiração (`exp`) for menor ou igual ao timestamp atual do navegador, o token é considerado inválido, as credenciais são apagadas do `localStorage` e a navegação é bloqueada.

* **Injeção Automática (Interceptors):** O cliente HTTP deve possuir um interceptor global que lê o token salvo no navegador e o anexa automaticamente no cabeçalho `Authorization: Bearer token` de cada requisição enviada ao backend.

* **Tratamento de Desautenticação (Response Interceptor):** O cliente HTTP deve interceptar erros do tipo `401 Unauthorized` de forma global nas respostas das requisições. Quando um erro 401 for detectado, o frontend deve limpar imediatamente as credenciais salvas no `localStorage` e forçar o redirecionamento para `/login`, apresentando um aviso visual de "Sessão expirada".



---

## 4. Estratégia de Ambientes (DEV vs PROD)

O frontend deve se comunicar de forma transparente com as portas e bancos específicos configurados na infraestrutura do backend utilizando os arquivos de configuração de ambiente do Vite (`.env`).

### 4.1. Arquivo de Desenvolvimento (`.env.development`)

Configura o app para consumir a API local de desenvolvimento.

```env
VITE_API_URL=http://localhost:5290
VITE_ENVIRONMENT=development

```

(Porta parametrizada conforme o perfil http do backend).

### 4.2. Arquivo de Produção (`.env.production`)

Configura o app para apontar para a URL final do backend em produção. Como o frontend roda diretamente no navegador do cliente (SPA), ele não pode apontar para `localhost`. Ele deve apontar para o domínio ou IP público/da rede interna onde a API do backend está hospedada e exposta.

```env
VITE_API_URL=https://api.juliusfinances.com
VITE_ENVIRONMENT=production

```

(A URL de API acima é ilustrativa e deve ser substituída pelo domínio de produção real no momento do deploy. Caso rode em ambiente de rede local integrada, pode ser o IP estático do servidor).

---

## 5. Sistema de Temas (Dark / Light Mode)

Para oferecer conforto visual em qualquer ambiente, a aplicação deve suportar a alternância dinâmica de cores entre o modo escuro (Dark) e claro (Light).

### 5.1. Mecanismo de Chaveamento do Tailwind e shadcn/ui

* O projeto deve ativar a estratégia baseada em classe no arquivo `tailwind.config.js` (`darkMode: 'class'`).
* Quando o modo escuro estiver ativo, a classe `.dark` será injetada na tag raiz `html` da aplicação. Os componentes e tokens de cor do shadcn/ui usarão o prefixo `dark:` do Tailwind para remapear as cores de fundo, bordas e textos de forma totalmente coordenada (ex: `bg-white dark:bg-slate-900`).

### 5.2. Persistência e Preferência do Sistema

1. **Primeiro Acesso:** O app deve ler a API nativa do navegador `window.matchMedia('(prefers-color-scheme: dark)')` para identificar se o sistema operacional do usuário já está em modo escuro e aplicar o tema correspondente por padrão.
2. **Persistência:** Caso o usuário mude manualmente o tema através de um botão de alternância (toggle) na interface, essa escolha deve ser salva no `localStorage` (ex: `theme: 'dark'`), sobrescrevendo a preferência do sistema operacional nas visitas futuras.

---

## 6. Robustez & Resiliência do Cliente (Tratamento de Falhas)

Para garantir uma experiência de usuário (UX) fluida e estável, o frontend deve implementar práticas defensivas e resilientes contra indisponibilidades e estados corrompidos.

### 6.1. Tratamento de Timeouts e Erros de Rede Física
* **Timeout de Requisição:** Todas as requisições HTTP feitas pela aplicação devem possuir um limite máximo de tempo de resposta configurado globalmente (ex: 15 segundos).
* **Indisponibilidade do Backend:** O interceptor de respostas do cliente HTTP deve monitorar falhas de rede física (cenários onde não há resposta HTTP do servidor, como `ERR_CONNECTION_REFUSED`, timeouts ou status 503/504).
* **Feedback ao Usuário:** Nesses cenários, a aplicação deve capturar o erro e disparar um alerta visual amigável e não-bloqueante (como um componente Toast informando "O servidor está temporariamente inacessível. Por favor, verifique sua conexão."). Isso impede que a interface fique congelada em estado de carregamento ou sem qualquer indicação visual de falha.

### 6.2. Proteção contra Tokens JWT Corrompidos no Armazenamento
* **Leitura Defensiva:** Qualquer leitura ou decodificação do token `accessToken` no `localStorage` realizada pelo Auth Guard ou interceptores deve ser encapsulada em blocos `try-catch`.
* **Recuperação Automática:** Caso o parse ou decodificação falhe (por exemplo, devido a um token corrompido ou string inválida injetada no armazenamento local), o erro deve ser capturado silenciosamente. O app deve limpar a chave `accessToken` do `localStorage` e redirecionar o usuário para a tela de `/login` de forma transparente, garantindo que o sistema se recupere e não trave por erros de exceção JavaScript na inicialização.