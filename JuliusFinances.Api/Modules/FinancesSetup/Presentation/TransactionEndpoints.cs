using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Exceptions;
using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Api.Modules.FinancesSetup.Presentation;

/// <summary>
/// Define os endpoints de rotas do módulo de Transações agrupados em /transactions.
/// </summary>
public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/transactions")
                       .RequireAuthorization()
                       .WithTags("Transactions");

        group.MapGet("/", ListAsync)
             .WithName("ListTransactions")
             .WithSummary("Listar transações paginadas")
             .WithDescription("Retorna as movimentações financeiras ativas do usuário autenticado de forma paginada e com filtros opcionais.")
             .Produces<IEnumerable<TransactionResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetByIdAsync)
             .WithName("GetTransactionById")
             .WithSummary("Obter transação por ID")
             .WithDescription("Retorna os detalhes de uma transação específica pertencente ao usuário autenticado.")
             .Produces<TransactionResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
             .WithName("CreateTransaction")
             .WithSummary("Criar transação")
             .WithDescription("Cria uma nova transação financeira de receita ou despesa vinculada a uma conta e categoria.")
             .Produces<TransactionResponse>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
             .WithName("UpdateTransaction")
             .WithSummary("Atualizar transação")
             .WithDescription("Atualiza os dados de uma transação ativa pertencente ao usuário autenticado.")
             .Produces<TransactionResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
             .WithName("DeleteTransaction")
             .WithSummary("Excluir/Arquivar transação")
             .WithDescription("Arquiva logicamente uma transação financeira pertencente ao usuário autenticado.")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransactionRepository transactionRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var resolvedPage = page.HasValue && page.Value >= 1 ? page.Value : 1;
        var resolvedPageSize = pageSize.HasValue && pageSize.Value >= 1 && pageSize.Value <= 100 ? pageSize.Value : 20;

        var transactions = await transactionRepository.GetPagedByUserIdAsync(
            userId.Value,
            resolvedPage,
            resolvedPageSize,
            accountId,
            categoryId,
            cancellationToken);

        var response = transactions.Select(MapToResponse);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransactionRepository transactionRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var transaction = await transactionRepository.GetByIdAsync(new TransactionId(id), cancellationToken);
        if (transaction == null || transaction.IsDeleted) return Results.NotFound();

        if (transaction.OwnerId.Value != userId.Value)
        {
            throw new TransactionForbiddenAccessException();
        }

        return Results.Ok(MapToResponse(transaction));
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateTransactionRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransactionRepository transactionRepository,
        [FromServices] IAccountRepository accountRepository,
        [FromServices] ICategoryRepository categoryRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        if (!Enum.TryParse<TransactionType>(request.Type, true, out var transactionType))
        {
            throw new DomainException("Tipo de transação inválido. Valores permitidos: Income, Expense.");
        }

        // Validação da Moeda (deve ser estritamente "BRL")
        if (request.Currency != "BRL")
        {
            throw new DomainException("A moeda da transação deve ser obrigatoriamente BRL.");
        }

        // Verifica a Conta
        var account = await accountRepository.GetByIdAsync(new AccountId(request.AccountId), cancellationToken);
        if (account == null) return Results.NotFound();
        if (account.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }
        if (account.IsDeleted)
        {
            throw new DomainException("Não é possível associar transações a uma conta arquivada.");
        }

        // Verifica a Categoria
        var category = await categoryRepository.GetByIdAsync(new CategoryId(request.CategoryId), cancellationToken);
        if (category == null) return Results.NotFound();
        if (category.OwnerId != null && category.OwnerId.Value != userId.Value)
        {
            throw new CategoryForbiddenAccessException();
        }
        if (category.IsDeleted)
        {
            throw new DomainException("Não é possível associar transações a uma categoria arquivada.");
        }

        // Instancia os Value Objects e a Entidade de Domínio
        var desc = new TransactionDescription(request.Description);
        var money = new Money(request.Amount, request.Currency);
        var transactionId = TransactionId.Unique();

        var transaction = new Transaction(
            transactionId,
            desc,
            transactionType,
            money,
            account.Id,
            category.Id,
            new OwnerId(userId.Value),
            request.TransactionDate,
            category.FlowType);

        await transactionRepository.AddAsync(transaction, cancellationToken);

        var response = MapToResponse(transaction);
        return Results.Created($"/transactions/{transaction.Id.Value}", response);
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateTransactionRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransactionRepository transactionRepository,
        [FromServices] IAccountRepository accountRepository,
        [FromServices] ICategoryRepository categoryRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        // Busca Inicial
        var transaction = await transactionRepository.GetByIdAsync(new TransactionId(id), cancellationToken);
        if (transaction == null || transaction.IsDeleted) return Results.NotFound();

        // Permissão
        if (transaction.OwnerId.Value != userId.Value)
        {
            throw new TransactionForbiddenAccessException();
        }

        // Validação da Moeda (deve ser estritamente "BRL")
        if (request.Currency != "BRL")
        {
            throw new DomainException("A moeda da transação deve ser obrigatoriamente BRL.");
        }

        // Verifica a Conta
        var account = await accountRepository.GetByIdAsync(new AccountId(request.AccountId), cancellationToken);
        if (account == null) return Results.NotFound();
        if (account.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }
        if (account.IsDeleted)
        {
            throw new DomainException("Não é possível associar transações a uma conta arquivada.");
        }

        // Verifica a Categoria
        var category = await categoryRepository.GetByIdAsync(new CategoryId(request.CategoryId), cancellationToken);
        if (category == null) return Results.NotFound();
        if (category.OwnerId != null && category.OwnerId.Value != userId.Value)
        {
            throw new CategoryForbiddenAccessException();
        }
        if (category.IsDeleted)
        {
            throw new DomainException("Não é possível associar transações a uma categoria arquivada.");
        }

        var desc = new TransactionDescription(request.Description);
        var money = new Money(request.Amount, request.Currency);

        transaction.Update(
            desc,
            money,
            account.Id,
            category.Id,
            request.TransactionDate,
            category.FlowType);

        transactionRepository.Update(transaction);

        return Results.Ok(MapToResponse(transaction));
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransactionRepository transactionRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var transaction = await transactionRepository.GetByIdAsync(new TransactionId(id), cancellationToken);
        if (transaction == null || transaction.IsDeleted) return Results.NotFound();

        if (transaction.OwnerId.Value != userId.Value)
        {
            throw new TransactionForbiddenAccessException();
        }

        transactionRepository.Delete(transaction);

        return Results.NoContent();
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }

    private static TransactionResponse MapToResponse(Transaction transaction)
    {
        return new TransactionResponse(
            transaction.Id.Value,
            transaction.Description.Value,
            transaction.Type.ToString(),
            new MoneyResponse(transaction.Money.Amount, transaction.Money.Currency),
            transaction.AccountId.Value,
            transaction.CategoryId.Value,
            transaction.TransactionDate);
    }
}

/// <summary>
/// Contrato de saída das respostas do módulo de transações.
/// </summary>
public record TransactionResponse(
    Guid Id,
    string Description,
    string Type,
    MoneyResponse Money,
    Guid AccountId,
    Guid CategoryId,
    DateTime TransactionDate);

/// <summary>
/// Contrato de saída composto para valores monetários.
/// </summary>
public record MoneyResponse(decimal Amount, string Currency);

/// <summary>
/// Contrato de entrada para a criação de transações.
/// </summary>
public record CreateTransactionRequest(
    string Description,
    string Type,
    decimal Amount,
    string Currency,
    Guid AccountId,
    Guid CategoryId,
    DateTime TransactionDate);

/// <summary>
/// Contrato de entrada para a atualização de transações.
/// </summary>
public record UpdateTransactionRequest(
    string Description,
    decimal Amount,
    string Currency,
    Guid AccountId,
    Guid CategoryId,
    DateTime TransactionDate);
