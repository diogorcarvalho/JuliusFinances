using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using JuliusFinances.Api.Common.Database;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Api.Modules.FinancesSetup.Presentation;

/// <summary>
/// Define os endpoints de agregação e resumo de dados do Dashboard em /dashboard.
/// </summary>
public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/dashboard")
                       .RequireAuthorization()
                       .WithTags("Dashboard");

        group.MapGet("/summary", GetSummaryAsync)
             .WithName("GetDashboardSummary")
             .WithSummary("Obter resumo do dashboard")
             .WithDescription("Retorna as métricas consolidadas, transações recentes e gastos por categoria do usuário autenticado.")
             .Produces<DashboardSummaryResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

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

        // NOTA DE ARQUITETURA:
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

        // 5. Gastos agregados por categoria no mês corrente (apenas contas ativas e agrupamento pelo objeto de valor Name)
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

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }
}

/// <summary>
/// Contrato de saída das respostas agregadas do Dashboard.
/// </summary>
public record DashboardSummaryResponse(
    decimal Balance,
    decimal Incomes,
    decimal Expenses,
    IEnumerable<RecentTransactionDto> RecentTransactions,
    IEnumerable<CategoryExpenseDto> CategoryExpenses);

/// <summary>
/// DTO simplificado para listagem de transações recentes no Dashboard.
/// </summary>
public record RecentTransactionDto(
    Guid Id,
    string Description,
    decimal Amount,
    string Type,
    string CategoryName,
    DateTime Date);

/// <summary>
/// DTO simplificado para exibição do progresso de orçamento de despesas por categoria.
/// </summary>
public record CategoryExpenseDto(
    Guid CategoryId,
    string CategoryName,
    decimal TotalSpent);
