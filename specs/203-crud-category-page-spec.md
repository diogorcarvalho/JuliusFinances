# 203 - Especificação da Criação da Página de Categorias no Front-end (JuliusFinances.Web)

Este documento estabelece as diretrizes funcionais, técnicas e de design de interface para a criação da página de gerenciamento de Categorias (CRUD) no front-end do JuliusFinances, integrando-a com as APIs de classificação financeira do back-end.

---

## 1. Regras de Negócio e Comportamento

A página de Categorias reflete diretamente as regras de visibilidade híbrida, restrições e integridade de dados modeladas no back-end:

### 1.1. Multi-inquilinato (Multi-tenant), Segurança e Visibilidade

* **Isolamento de Dados:** Exibição exclusiva de dados do usuário autenticado. O token JWT é inserido automaticamente no cabeçalho das requisições pelo `apiClient`.

* **Categorias Globais vs. Pessoais:**
  * As **Categorias Globais** (onde `isGlobal == true` ou `OwnerId == null` no banco) são estritamente **somente leitura** na UI. O usuário pode visualizá-las e filtrá-las, mas os botões de edição (ícone) e exclusão (lixeira) devem ser ocultados ou desabilitados para estes registros.
  * As **Categorias Pessoais** (onde `isGlobal == false`) são de propriedade do usuário e permitem edição e exclusão completas.

### 1.2. Validações e Higienização do Formulário de Criação/Edição

* **Nome da Categoria:** Obrigatório, com tamanho mínimo de 3 caracteres e máximo de 100 caracteres. Para simetria com as validações de duplicidade no banco, o front-end deve sanitizar o texto antes do envio, removendo espaços duplicados e aplicando normalização de capitalização elegante:

```typescript
const normalizedName = name.trim().replace(/\s+/g, ' ');
```

* **Tipo de Fluxo (`FlowType`):** Seleção obrigatória entre `Income` (Entrada/Receita), `Expense` (Saída/Despesa) ou `Both` (Ambos/Transferências e Ajustes).

* **Customização Visual Dinâmica (Pure Front-end):**
  Como o modelo do back-end (`Category.cs`) não persiste dados de customização no banco, o front-end deve mapear visualmente as categorias de forma dinâmica no lado do cliente:
  * **Mapeamento por Tipo (`FlowType`):**
    * `Income` (Receita): Destaques em Verde (ex: Tailwind `emerald-500`), utilizando ícones dinâmicos de entrada como `TrendingUp` ou `ArrowUpRight` da biblioteca Lucide.
    * `Expense` (Despesa): Destaques em Vermelho (ex: Tailwind `rose-500`), utilizando ícones dinâmicos de saída como `TrendingDown` ou `ArrowDownRight` da biblioteca Lucide.
    * `Both` (Sistema/Ajustes/Duplo): Destaques em Cinza/Indigo (ex: Tailwind `slate-500`), utilizando ícones neutros como `ArrowLeftRight` ou `Settings` da biblioteca Lucide.
  * **Mapeamento por Palavra-Chave (UX Premium):** O front-end pode adotar um dicionário simples baseado no nome da categoria para selecionar ícones representativos (ex: `"alimentacao"` -> `Utensils`, `"mercado"` -> `ShoppingBag`, `"transporte"` -> `Car`, `"salario"` -> `DollarSign`, `"saude"` -> `Heart`, etc.), enriquecendo o design sem onerar o banco de dados.

### 1.3. Regras para Edição e Exclusão Segura

* **Validação de Duplicidade:** Caso o usuário tente salvar uma categoria com o mesmo nome de uma já ativa no seu escopo pessoal ou global, a API retornará HTTP `409 Conflict`. O front-end deve capturar esse erro e exibir um alerta focado no campo correspondente.

* **Integridade na Exclusão (Bloqueio estrito):** Se o usuário tentar remover uma categoria pessoal que possua transações vinculadas, o back-end impedirá terminantemente a operação devido a restrições de integridade financeira, retornando HTTP `400 Bad Request`. O front-end deve interceptar esse cenário e notificar o usuário com clareza:

> *"Atenção: Esta categoria não pode ser excluída porque possui transações financeiras vinculadas no seu histórico. Para poder excluí-la, você deve primeiro alterar a categoria ou excluir as transações correspondentes."*

---

## 2. Design Visual e Experiência do Usuário (UI/UX)

O desenvolvimento visual deve adotar a abordagem mobile-first, suporte a Dark/Light Mode e ser construído usando marcações nativas do TailwindCSS e estados de React (mantendo consistência técnica e estética com `AccountsView.tsx`), evitando a introdução de frameworks de terceiros ou `shadcn/ui` que não façam parte das dependências nativas do projeto.

### 2.1. Estrutura de Abas (Tabs Filter)

Para facilitar a navegação do usuário, a lista de categorias deve ser segmentada em abas utilizando botões customizados com controle de estado nativo:

1. **Todas:** Exibe o conjunto unificado de categorias globais e pessoais.
2. **Receitas (`Income`):** Filtra apenas categorias elegíveis para entradas.
3. **Despesas (`Expense`):** Filtra apenas categorias elegíveis para saídas.
4. **Sistema/Especiais (`Both`):** Filtra categorias especiais (como ajustes e transferências).

### 2.2. Cartões de Categoria (Grid Layout)

Cada categoria deve ser renderizada como um card dinâmico em um grid responsivo:

* **Cor de Fundo e Ícone:** O card deve utilizar a cor de destaque dinâmica baseada no `FlowType` ou correspondência de palavras-chave do nome da categoria, exibindo o glifo Lucide resolvido de forma suave e elegante.
* **Badge de Tipo:** Um pequeno indicador visual indicando a abrangência:
  * Badge Verde para *Receita*.
  * Badge Vermelho para *Despesa*.
  * Badge Cinza para *Duplo/Sistema*.
* **Indicador Global vs. Pessoal:** Categorias de sistema (Globais) devem exibir um selo discreto (ex: "Sistema" ou "Global") para justificar visualmente a ausência das ações de alteração.
* **Ações Rápidas:** Botões de edição e exclusão posicionados de forma limpa, aparecendo apenas nas categorias pessoais.

### 2.3. Modal de Cadastro/Edição

* Utilização de backdrop desfocado (`backdrop-blur-sm`) controlado através de estados puros de React (`isOpen`).
* **Prevenção de Cliques Duplos (Double Submission):** Controle de estado `isSubmitting` para desabilitar o formulário e os botões de ação (incluindo o botão de fechar o modal) durante o envio da requisição, exibindo um spinner animado (`Loader2` do lucide-react) no botão de salvar, blindando a persistência contra requisições repetidas.

### 2.4. Estado Vazio (Empty State)

* Caso o usuário remova todas as categorias pessoais e o filtro de abas resulte em vazio, exibir uma ilustração minimalista e uma mensagem motivadora com chamada para ação: *"Crie sua primeira categoria personalizada para detalhar ainda mais seus gastos!"*

---

## 3. APIs Integradas

O front-end conecta-se às seguintes rotas expostas pelo back-end no grupo `/categories`:

* **Listagem Unificada:** `GET /categories` (Retorna a mescla de globais e pessoais do usuário).
* **Detalhes por ID:** `GET /categories/{id}`.
* **Criação:** `POST /categories` enviando `{ name, flowType }`.
* **Atualização:** `PUT /categories/{id}` enviando `{ name, flowType }`.
* **Remoção:** `DELETE /categories/{id}`.

---

## 4. Estabilidade de Rede, Cancelamentos e Integração de Navegação

Seguindo o padrão de estabilidade e resiliência adotado no projeto:

1. **Prevenção de chamadas duplicadas (React Strict Mode):** O hook de efeito ou o query manager que realiza a busca das categorias em `useEffect` deve gerenciar um `AbortController` nativo.
2. **Ignorar Cancelamentos:** O interceptor ou o bloco de captura de erros do componente deve usar o utilitário `axios.isCancel(error)` para abortar de forma silenciosa requisições limpas pelo ciclo de vida do React, evitando alertas falsos de "indisponibilidade do servidor".
3. **Integração de Rota e Layout:**
   * **Rotas (`App.tsx`):** Registrar a rota privada `/categories` associando-a à view `CategoriesView` sob a estrutura de roteamento privada.
   * **Layout de Navegação (`Layout.tsx`):** Adicionar no array de `navItems` o atalho para a página de Categorias, garantindo que o usuário possa acessar a funcionalidade tanto na barra lateral (desktop) quanto no menu inferior (mobile):
     ```typescript
     { label: 'Categorias', path: '/categories', icon: Tag }
     ```

---
