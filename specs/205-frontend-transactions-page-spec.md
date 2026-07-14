# 203 - Especificação da Criação da Página de Transações no Front-end (JuliusFinances.Web)

Este documento estabelece as diretrizes funcionais, técnicas e de design de interface para a implementação da página de **Transações e Transferências** no front-end do JuliusFinances, integrando-a de forma robusta com as APIs de movimentações financeiras pessoais.

---

## 1. Regras de Negócio e Comportamento

A página de Transações consolida o fluxo de entradas (receitas), saídas (despesas) e transferências entre contas do usuário, respeitando as seguintes regras do domínio de negócio:

### 1.1. Multi-inquilinato (Multi-tenant) e Segurança

* Exibição estritamente focada nas movimentações do usuário autenticado. O token JWT anexado pelo `apiClient` garante que nenhuma requisição acesse dados de terceiros.


* Se o token expirar ou for inválido, a aplicação limpa o `localStorage` e redireciona de forma defensiva para `/login`.



### 1.2. Integração e Dependências de Formulário

O formulário de registro de movimentações possui alta dependência de outros módulos do sistema. Antes de abrir o modal de criação/edição, o frontend deve:

* **Carregar Contas Ativas:** Popular o seletor de contas consumindo a rota `GET /accounts`. Contas arquivadas (`IsDeleted == true`) não devem constar como opção para novas transações.


* **Carregar Categorias Ativas:** Popular o seletor de categorias consumindo a rota `GET /categories`. O frontend deve filtrar e exibir apenas as categorias compatíveis com o tipo de fluxo selecionado:


* Se o tipo for **Despesa (Expense)**, exibe categorias de fluxo `Expense` ou `Both`.


* Se o tipo for **Receita (Income)**, exibe categorias de fluxo `Income` ou `Both`.


* Se o tipo for **Transferência (Transfer)**, o seletor de categorias é ocultado, pois o backend vincula automaticamente a categoria padrão de transferência.





### 1.3. Validações Locais (Client-Side)

* **Descrição da Transação:** Obrigatória, de 3 a 250 caracteres. Deve ser higienizada para remover múltiplos espaços em branco consecutivos.


* **Valor Monetário (`Amount`):** Deve ser maior que zero e menor ou igual a `99.999,999,999.99`. Valores negativos ou zerados são bloqueados nativamente pela validação do formulário.


* **Moeda:** Fixada implicitamente como `"BRL"`.


* **Contas de Origem e Destino (Exclusivo de Transferências):** O formulário deve garantir que a conta de origem seja obrigatoriamente diferente da conta de destino. Caso o usuário selecione a mesma conta em ambos os seletores, um alerta visual de impedimento deve ser renderizado imediatamente, desabilitando o envio.


* **Limite Temporal de Data:** O input de data da movimentação deve restringir o ano entre **2000 e 2100** para evitar erros de estouro de dados contábeis no banco.


* **Fuso Horário (UTC):** Toda data de transação selecionada no fuso local do navegador do usuário deve ser convertida para o formato ISO UTC (Z) antes de ser enviada nos payloads de `POST` ou `PUT`.



### 1.4. Estrutura de Abas, Paginação e Filtros

Para garantir integridade contábil e compatibilidade com os endpoints segregados do backend, a tela principal é dividida em duas abas (Tabs) principais:

1. **Aba "Transações":** Consome e gerencia exclusivamente receitas e despesas.
   * **Listagem e Paginação:** Utiliza paginação dinâmica via `GET /transactions` (padrão de 20 itens por página).
   * **Filtros Dinâmicos:** Permite filtrar por conta (`accountId`) e categoria (`categoryId`). A alteração de qualquer filtro reinicia a paginação para a primeira página.

2. **Aba "Transferências":** Consome e gerencia exclusivamente transferências de saldo entre contas próprias.
   * **Listagem e Paginação:** Utiliza paginação dinâmica via `GET /transfers` (padrão de 20 itens por página).
   * **Filtros Dinâmicos:** Permite filtrar por conta de origem/destino (`accountId`). A alteração do filtro reinicia a paginação para a primeira página.

Cada aba mantém seu próprio estado isolado de paginação e filtros na memória do React, evitando chamadas desnecessárias ou estados misturados.

---

## 2. Design Visual e Experiência do Usuário (UI/UX)

O layout foi concebido de forma totalmente responsiva (mobile-first), com transição fluida entre os modos Claro e Escuro.

### 2.1. Visão Geral da Tela (Mobile ao Desktop)

A tela principal apresenta um controle de Abas (Tabs) no topo para alternar de forma coesa entre "Transações" e "Transferências".

* **Smartphone (Mobile):** Filtros condensados em um botão do tipo gatilho que abre uma gaveta inferior (Drawer) específica para a aba ativa. A listagem é disposta em formato de lista de cartões (Cards) compactos agrupados por data.
* **Desktop:**
  * **Aba Transações:** Painel lateral de filtros (Conta e Categoria) fixo na esquerda e tabela financeira rica à direita, contendo colunas explícitas de: Data, Descrição, Categoria, Conta, Tipo e Valor.
  * **Aba Transferências:** Painel lateral de filtros (Conta de Origem/Destino) fixo na esquerda e tabela de transferências à direita, contendo colunas explícitas de: Data, Descrição, Conta de Origem, Conta de Destino e Valor.

### 2.2. Identificação Visual das Movimentações

Os valores numéricos e status visuais utilizam as seguintes cores e convenções:

* **Receitas (Incomes):** Texto verde (`text-emerald-600` / `dark:text-emerald-400`) acompanhado de um indicador de soma (`+ R$ 250,00`).
* **Despesas (Expenses):** Texto vermelho/coral (`text-rose-600` / `dark:text-rose-400`) acompanhado de um indicador de subtração (`- R$ 89,90`).
* **Transferências (Transfers):** Texto cinza ou azulado (`text-slate-600` / `dark:text-slate-400`), indicando a conta de origem e destino na própria descrição de forma intuitiva (ex: *Itaú ➔ Carteira*).

### 2.3. Formulário de Cadastro Unificado (Tabs Interativas)

Para melhorar a usabilidade e evitar modais excessivos, a inclusão é realizada através de um único Modal contendo uma interface de abas (Tabs) no topo:

1. **Aba 1: Transação** (Gerencia despesas ou receitas).


2. **Aba 2: Transferência** (Gerencia a movimentação simétrica de fundos entre contas próprias).



Durante a submissão, todos os campos e o botão principal mudam para o estado desativado (`disabled`), exibindo um indicador visual de carregamento para blindar a API contra cliques duplos acidentais (Double Submission).

---

## 3. APIs Integradas

O módulo consome as seguintes rotas do backend do JuliusFinances:

### 3.1. Dados de Suporte (Preenchimento de Formulários)

* `GET /accounts` — Obtém a listagem de contas ativas do usuário.


* `GET /categories` — Obtém a listagem de categorias acessíveis (Globais + Pessoais).



### 3.2. Operações de Transações convencionais

* `GET /transactions?page=1&pageSize=20` — Obtém a lista paginada e filtrada de receitas e despesas.


* `POST /transactions` — Cria uma nova transação.


* `PUT /transactions/{id}` — Atualiza os dados de uma transação ativa.


* `DELETE /transactions/{id}` — Realiza o arquivamento (Soft-Delete) da transação.



### 3.3. Operações de Transferências

* `GET /transfers?page=1&pageSize=20` — Obtém a lista paginada de transferências de saldo.


* `POST /transfers` — Cria uma nova transferência.


* `PUT /transfers/{id}` — Atualiza os dados de uma transferência ativa.


* `DELETE /transfers/{id}` — Realiza o arquivamento (Soft-Delete) da transferência.



---

## 4. Plano de Verificação e Testes

* **Teste de Integridade de Soft-Delete:** Garantir que transações excluídas sumam imediatamente da listagem ativa sem que ocorram falhas ou dízimas de arredondamento em tempo real nos saldos parciais exibidos.


* **Teste de Condições de Corrida (Strict Mode):** Verificar o comportamento da paginação em desenvolvimento local para certificar-se de que o uso do `AbortController` cancela requisições antigas se o usuário mudar de página rapidamente ou alternar entre filtros freneticamente.


* **Teste de Margem de Fusos Horários:** Registrar uma transação às 23:30 no fuso local (UTC-3) e certificar-se de que o sistema exiba a data no dia correto, sem transbordar para o dia seguinte ou anterior de forma errática devido ao tratamento inadequado do backend (que exige persistência puramente em UTC).