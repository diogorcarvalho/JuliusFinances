# Integração do Dashboard com as API's

Este documento estabelece as diretrizes funcionais e técnicas para integrar a tela de Dashboard com o backend do JuliusFinances, migrando a exibição de mocks estáticos para dados reais em tempo de execução.

---

## 1. Regras de Negócio e Comportamento

### 1.1. Multi-inquilinato (Multi-tenant) e Segurança
- O Dashboard exibe estritamente dados pessoais. O usuário autenticado só pode visualizar saldos, receitas, despesas e transações das quais for proprietário (`OwnerId` correspondente ao ID do token JWT).
- Qualquer requisição sem token válido retornará `401 Unauthorized`.

### 1.2. Regra Matemática de Saldo Geral (Preservação de Integridade de Soft-Delete)
O saldo consolidado do usuário é calculado de forma agregada no banco de dados PostgreSQL:
- **Saldo Geral** = `Sum(InitialBalance de contas ativas)` + `Sum(Receitas ativas associadas a contas ativas)` - `Sum(Despesas ativas associadas a contas ativas)`.
- *Nota Crítica:* Para evitar corrupção matemática do saldo histórico, transações pertencentes a contas que foram arquivadas (`IsDeleted == true`) **devem ser totalmente desconsideradas** no cálculo do saldo consolidado e de relatórios mensais.
- *Nota de Transferências:* Transferências entre contas próprias (`IsDeleted == false`) possuem débito e crédito simétricos e se anulam, não afetando o patrimônio líquido geral do usuário.

### 1.3. Competência Mensal de Receitas e Despesas
- Os cards de "Receitas (Mês)" e "Despesas (Mês)" contabilizam apenas transações ativas (`IsDeleted == false`) pertencentes a contas ativas (`IsDeleted == false`) cujo `TransactionDate` ocorra dentro do mês e ano UTC correntes.

### 1.4. Lista de Transações Recentes
- Exibe as **últimas 5 movimentações** (receitas ou despesas ativas) pertencentes a contas ativas do usuário, ordenadas de forma decrescente por `TransactionDate` e desempatadas por `CreatedAt`.
- A API resolve e retorna o nome da categoria associada utilizando junção direta (`Join`) no banco de dados.

### 1.5. Limites de Orçamento (Orçamento Limite)
- O backend calcula os gastos acumulados do mês corrente por categoria ativa. O frontend estabelece limites de orçamento estáticos para fins de exibição dos progressos:
  - **Alimentação:** R$ 1.200,00
  - **Habitação:** R$ 2.500,00
  - **Entretenimento:** R$ 300,00
- O progresso é calculado no frontend como `(GastoReal / Limite) * 100`.

---

## 2. Nova API de Resumo do Dashboard (`GET /dashboard/summary`)

Para maximizar a eficiência, a API agregará todas as informações necessárias em uma única requisição.

### 2.1. Estrutura de Contratos (DTOs)

```csharp
public record DashboardSummaryResponse(
    decimal Balance,
    decimal Incomes,
    decimal Expenses,
    IEnumerable<RecentTransactionDto> RecentTransactions,
    IEnumerable<CategoryExpenseDto> CategoryExpenses);

public record RecentTransactionDto(
    Guid Id,
    string Description,
    decimal Amount,
    string Type, // "income" ou "expense"
    string CategoryName,
    DateTime Date);

public record CategoryExpenseDto(
    Guid CategoryId,
    string CategoryName,
    decimal TotalSpent);
```

### 2.2. Lógica de Consulta (LINQ/EF Core)

```csharp
// Localização: JuliusFinances.Api/Modules/FinancesSetup/Presentation/DashboardEndpoints.cs

/// <summary>
/// Retorna o resumo consolidado do dashboard para o usuário autenticado de forma sequencial e thread-safe.
/// </summary>
private static async Task<IResult> GetSummaryAsync(
    ClaimsPrincipal claimsPrincipal,
    [FromServices] JuliusDbContext dbContext,
    CancellationToken cancellationToken)
{
    var userId = GetUserId(claimsPrincipal);
    if (userId == null) return Results.Unauthorized();

    var ownerId = new OwnerId(userId.Value);
    var nowUtc = DateTime.UtcNow;
    var firstDayOfMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    // NOTA DE DESEMPENHO E ARQUITETURA:
    // O DbContext do EF Core NÃO é thread-safe. As consultas abaixo devem ser executadas 
    // de forma estritamente sequencial para evitar erros de concorrência.

    // 1. Saldo inicial total das contas ativas do usuário
    var initialBalance = await dbContext.Accounts
        .Where(a => a.OwnerId == ownerId && !a.IsDeleted)
        .SumAsync(a => a.InitialBalance, cancellationToken);

    // 2. Histórico de transações ativas pertencentes a contas ATIVAS para cálculo do Saldo Geral
    var totalIncomes = await (
        from t in dbContext.Transactions
        join a in dbContext.Accounts on t.AccountId equals a.Id
        where t.OwnerId == ownerId && !t.IsDeleted && !a.IsDeleted && t.Type == TransactionType.Income
        select t.Money.Amount
    ).SumAsync(cancellationToken);

    var totalExpenses = await (
        from t in dbContext.Transactions
        join a in dbContext.Accounts on t.AccountId equals a.Id
        where t.OwnerId == ownerId && !t.IsDeleted && !a.IsDeleted && t.Type == TransactionType.Expense
        select t.Money.Amount
    ).SumAsync(cancellationToken);

    var currentBalance = initialBalance + totalIncomes - totalExpenses;

    // 3. Receitas e despesas do mês corrente (apenas contas ativas)
    var monthIncomes = await (
        from t in dbContext.Transactions
        join a in dbContext.Accounts on t.AccountId equals a.Id
        where t.OwnerId == ownerId && !t.IsDeleted && !a.IsDeleted && t.Type == TransactionType.Income && t.TransactionDate >= firstDayOfMonth
        select t.Money.Amount
    ).SumAsync(cancellationToken);

    var monthExpenses = await (
        from t in dbContext.Transactions
        join a in dbContext.Accounts on t.AccountId equals a.Id
        where t.OwnerId == ownerId && !t.IsDeleted && !a.IsDeleted && t.Type == TransactionType.Expense && t.TransactionDate >= firstDayOfMonth
        select t.Money.Amount
    ).SumAsync(cancellationToken);

    // 4. Últimas 5 transações com Join de Categorias e de Contas Ativas
    var recentTransactions = await (
        from t in dbContext.Transactions
        join a in dbContext.Accounts on t.AccountId equals a.Id
        join c in dbContext.Categories on t.CategoryId equals c.Id
        where t.OwnerId == ownerId && !t.IsDeleted && !a.IsDeleted
        orderby t.TransactionDate descending, t.CreatedAt descending
        select new RecentTransactionDto(
            t.Id.Value,
            t.Description.Value,
            t.Money.Amount,
            t.Type.ToString().ToLower(),
            c.Name.Value,
            t.TransactionDate)
    ).Take(5).ToListAsync(cancellationToken);

    // 5. Gastos agregados por categoria no mês corrente (apenas contas ativas e agrupado de forma segura pelo objeto de valor Name)
    var categoryExpenses = await (
        from t in dbContext.Transactions
        join a in dbContext.Accounts on t.AccountId equals a.Id
        join c in dbContext.Categories on t.CategoryId equals c.Id
        where t.OwnerId == ownerId && !t.IsDeleted && !a.IsDeleted && t.Type == TransactionType.Expense && t.TransactionDate >= firstDayOfMonth
        group t by new { c.Id, c.Name } into g
        select new CategoryExpenseDto(
            g.Key.Id.Value,
            g.Key.Name.Value,
            g.Sum(x => x.Money.Amount))
    ).ToListAsync(cancellationToken);

    var summary = new DashboardSummaryResponse(
        currentBalance,
        monthIncomes,
        monthExpenses,
        recentTransactions,
        categoryExpenses);

    return Results.Ok(summary);
}
```

---

## 3. Integração e Fluxo do Frontend

### 3.1. Chamada de API e Estados
- `DashboardView.tsx` deve invocar o endpoint `/dashboard/summary` via `apiClient`.
- Implementar estados de carregamento (`isLoading`) e skeletons nos componentes de cards de resumo, listas e progressos para evitar saltos visuais.
- Tratar cenários de erro de conexão exibindo alertas amigáveis e não-bloqueantes.

### 3.2. Renderização de Progresso de Orçamentos com Normalização de String
- Chaves predefinidas de limite (Ex: `alimentacao: 1200`, `habitacao: 2500`, `entretenimento: 300`) serão comparadas com os valores reais recebidos no array `categoryExpenses`.
- **Regra de Normalização:** Para evitar falhas silenciosas de mapeamento devido a acentuações ou capitalizações diferentes (ex: "Alimentação", "alimentacao", "Alimentacao"), as chaves das categorias devem ser normalizadas no frontend antes da comparação:
  ```typescript
  const normalizeString = (str: string) => 
    str.normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLowerCase().trim();
  ```
- Injeção dinâmica da porcentagem na largura das barras de progresso do Tailwind, estilizando as cores de acordo com o limite de gastos:
  - **Abaixo de 70%:** `bg-indigo-500` / `bg-emerald-500`
  - **Entre 70% e 90%:** `bg-amber-500`
  - **Acima de 90%:** `bg-rose-500` de alerta

---

## 4. Plano de Verificação e Testes

- **Testes Unitários:** Validar o cálculo matemático do saldo geral consolidado garantindo o isolamento correto de inquilinos e a desconsideração de transferências no saldo geral do usuário.
- **Caso de Teste Especial (Soft-Delete de Contas):** Verificar se a desativação/arquivamento de uma conta de usuário remove de forma bem-sucedida e imediata as transações vinculadas a ela do cálculo de saldo consolidado geral do dashboard.
- **Caso de Teste de Fuso Horário (Edge Case):** Simular cenários de transações nos extremos de datas limites do mês (ex: dia 1º de um mês no fuso UTC-3 que se traduz para o mês anterior em UTC) e assegurar que as definições de início do mês em UTC se mantêm íntegras.
- **Testes de UI:** Verificar o comportamento correto em telas limpas (usuário novo sem lançamentos, onde receitas/despesas/saldo devem constar como R$ 0,00 e exibir mensagens de "Nenhuma transação recente").
