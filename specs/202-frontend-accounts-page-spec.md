# 202 - Especificação da Criação da Página de Contas no Front-end (JuliusFinances.Web)

Este documento estabelece as diretrizes funcionais, técnicas e de design de interface implementadas para a criação da página de gerenciamento de Contas e Carteiras no front-end do JuliusFinances, integrando-a com as APIs de contas pessoais.

---

## 1. Regras de Negócio e Comportamento

A página de Contas reflete estritamente as especificações de negócio estabelecidas na modelagem do domínio no backend (especificação 004):

### 1.1. Multi-inquilinato (Multi-tenant) e Segurança
- Exibição de dados estritamente pessoais do usuário autenticado. O token JWT armazenado localmente no navegador é anexado automaticamente em todas as requisições pelo `apiClient`.
- Caso o token esteja expirado ou ausente, a navegação é redirecionada para `/login`.

### 1.2. Validações do Formulário de Criação/Edição e Normalizações
- **Nome da Conta:** Obrigatório, com tamanho mínimo de 3 e máximo de 100 caracteres. Para total simetria com a validação do backend (e impedir evasão de chaves duplicadas), o frontend executa a higienização de espaços sobressalentes e múltiplos espaços internos:
  ```typescript
  const normalizedName = name.trim().replace(/\s+/g, ' ');
  ```
- **Tipo de Conta:** Seleção entre as opções `CheckingAccount` (Conta Corrente), `SavingsAccount` (Poupança), `Investment` (Investimento) e `Cash` (Dinheiro / Carteira Física).
- **Saldo Inicial:** Obrigatório. Caso o tipo de conta selecionado seja `Cash` (Dinheiro em Espécie), o formulário impede preventivamente o preenchimento de saldos iniciais negativos (lançando aviso amigável).
- **Precisão Centesimal Decimal:** Para blindar os inputs de moeda contra imprecisões de dízimas periódicas nativas do IEEE 754 de ponto flutuante do JavaScript, os dados numéricos são normalizados para duas casas decimais fixas antes do envio:
  ```typescript
  const balanceValue = parseFloat(parsedBalance.toFixed(2));
  ```
- **Edição de Saldo Inicial:** O backend impede a alteração do saldo inicial caso a conta possua transações vinculadas. O frontend exibe uma nota informativa explicativa no modal sobre essa regra de integridade histórica.

### 1.3. Fluxo Inteligente de Exclusão (Soft Delete vs Hard Delete)
- Ao solicitar a remoção de uma conta, o sistema exibe uma caixa de confirmação informativa:
  > *"Caso esta conta possua transações registradas, ela será arquivada (arquivamento lógico) para preservar o histórico e a integridade de seus relatórios financeiros."*
- O backend realiza a exclusão física (`DELETE`) caso não existam transações, ou arquivamento (`IsDeleted = true`) se houver movimentações, garantindo a integridade dos dados sem corromper o histórico do dashboard.

---

## 2. Design Visual e Experiência do Usuário (UI/UX)

A interface foi construída utilizando Tailwind CSS, alinhada à estética moderna e limpa do projeto, suportando integralmente os modos Claro (Light) e Escuro (Dark).

### 2.1. Cartões de Identificação Visual (Grid)
Cada tipo de conta possui uma estilização única de ícones e cores para facilitar a identificação visual rápida pelo usuário:
- **Conta Corrente (`CheckingAccount`):** Tom azul com ícone `Landmark`.
- **Poupança (`SavingsAccount`):** Tom verde com ícone `PiggyBank`.
- **Investimento (`Investment`):** Tom roxo com ícone `Briefcase`.
- **Dinheiro / Carteira (`Cash`):** Tom âmbar com ícone `Wallet`.

Cada cartão apresenta:
- Título/Nome da conta em destaque.
- Tipo de conta amigável (traduzido).
- Saldo inicial formatado como moeda nacional (BRL `R$`).
- Ações rápidas de edição (ícone `Pencil`) e exclusão (ícone `Trash2`).

### 2.2. Modal Interativo de Formulário
- Utiliza backdrop de desfoque (`backdrop-blur-sm`) e transições animadas.
- Centraliza os campos de Nome, Tipo de Conta e Saldo Inicial.
- Apresenta feedback visual de envio (botão de carregamento com spinner `Loader2`).
- **Prevenção de Cliques Duplos (Double Submission):** Durante o envio do formulário, o estado `isSubmitting` desabilita todos os campos e os botões do formulário, blindando a transação contra condições de corrida.

### 2.3. Estado Vazio (Empty State)
- Caso o usuário não tenha nenhuma conta registrada, é renderizado um painel acolhedor com ilustração de carteira e um botão de chamada de ação proeminente: "Cadastrar Primeira Conta".

---

## 3. APIs Integradas

O frontend integra-se com as seguintes rotas Minimal APIs expostas pelo backend no grupo `/accounts`:

- **Listagem:** `GET /accounts` (Mapeia para `Account[]` local).
- **Criação:** `POST /accounts` com corpo `{ name, type, initialBalance }`.
- **Atualização:** `PUT /accounts/{id}` com corpo `{ name, type, initialBalance }`.
- **Exclusão/Arquivamento:** `DELETE /accounts/{id}`.

---

## 4. Correção e Estabilização: Tratamento de Cancelamentos do Axios

Durante a integração, foi detectado e resolvido um problema onde alertas visuais de rede ("O servidor está temporariamente inacessível...") apareciam ao navegar rápido entre as páginas do sistema.

- **Causa:** O React em Strict Mode (ou na transição de rotas) aborta chamadas pendentes via `AbortController`. O Axios rejeita essas requisições como erro, mas sem um objeto `response`. O interceptor global interpretava qualquer erro sem `response` como falha física de rede.
- **Solução:** Adicionamos uma regra prioritária no interceptor de respostas (`client.ts`) utilizando `axios.isCancel(error)` para abortar silenciosamente requisições canceladas, restabelecendo a fluidez e a estabilidade visual do aplicativo.
