using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Exceptions;
using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Api.Common.Database;

namespace JuliusFinances.Api.Modules.FinancesSetup.Presentation;

/// <summary>
/// Define os endpoints de rotas do módulo de Contas agrupados em /accounts.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts")
                       .RequireAuthorization()
                       .WithTags("Accounts");

        group.MapGet("/", ListAsync)
             .WithName("ListAccounts")
             .WithSummary("Listar contas ativas")
             .WithDescription("Retorna uma lista contendo as contas ativas de propriedade do usuário autenticado.")
             .Produces<IEnumerable<AccountResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetByIdAsync)
             .WithName("GetAccountById")
             .WithSummary("Obter conta por ID")
             .WithDescription("Retorna os detalhes de uma conta específica, validando se ela pertence ao usuário autenticado.")
             .Produces<AccountResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
             .WithName("CreateAccount")
             .WithSummary("Criar conta pessoal")
             .WithDescription("Cria uma nova conta pessoal associada ao usuário autenticado, validando as regras de duplicidade de nome e saldo inicial do tipo Cash.")
             .Produces<AccountResponse>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
             .WithName("UpdateAccount")
             .WithSummary("Atualizar conta pessoal")
             .WithDescription("Atualiza o nome, tipo ou saldo inicial (se não houver transações) de uma conta pessoal pertencente ao usuário autenticado.")
             .Produces<AccountResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteAsync)
             .WithName("DeleteAccount")
             .WithSummary("Excluir/Arquivar conta pessoal")
             .WithDescription("Exclui uma conta pessoal do usuário autenticado de forma física (se não houver transações) ou lógica (Soft Delete, caso possua movimentações).")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal claimsPrincipal,
        [FromServices] IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var accounts = await accountRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        var response = accounts.Select(MapToResponse);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var account = await accountRepository.GetByIdAsync(new AccountId(id), cancellationToken);
        if (account == null) return Results.NotFound();

        if (account.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }

        return Results.Ok(MapToResponse(account));
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateAccountRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
        {
            throw new DomainException("Tipo de conta inválido. Valores permitidos: CheckingAccount, SavingsAccount, Investment, Cash.");
        }

        var accountName = new AccountName(request.Name);

        var exists = await accountRepository.ExistsByNameAsync(accountName, userId.Value, cancellationToken);
        if (exists)
        {
            throw new AccountNameAlreadyExistsException(accountName.Value);
        }

        var account = new Account(
            AccountId.Unique(),
            accountName,
            accountType,
            request.InitialBalance,
            new OwnerId(userId.Value));

        await accountRepository.AddAsync(account, cancellationToken);

        var response = MapToResponse(account);
        return Results.Created($"/accounts/{account.Id.Value}", response);
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateAccountRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var account = await accountRepository.GetByIdAsync(new AccountId(id), cancellationToken);
        if (account == null) return Results.NotFound();

        if (account.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }

        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
        {
            throw new DomainException("Tipo de conta inválido. Valores permitidos: CheckingAccount, SavingsAccount, Investment, Cash.");
        }

        var newName = new AccountName(request.Name);

        if (account.Name.GetNormalizedForComparison() != newName.GetNormalizedForComparison())
        {
            var exists = await accountRepository.ExistsByNameAsync(newName, userId.Value, cancellationToken);
            if (exists)
            {
                throw new AccountNameAlreadyExistsException(newName.Value);
            }
        }

        var hasTransactions = false;
        if (request.InitialBalance != account.InitialBalance)
        {
            hasTransactions = await accountRepository.HasLinkedTransactionsAsync(account.Id, cancellationToken);
            if (hasTransactions)
            {
                throw new DomainException("O saldo inicial não pode ser alterado após o registro de transações.");
            }
        }

        account.Update(newName, accountType, request.InitialBalance, hasTransactions);
        accountRepository.Update(account);

        return Results.Ok(MapToResponse(account));
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] IAccountRepository accountRepository,
        [FromServices] JuliusDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var account = await accountRepository.GetByIdAsync(new AccountId(id), cancellationToken);
        if (account == null) return Results.NotFound();

        if (account.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }

        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var hasTransactions = await accountRepository.HasLinkedTransactionsAsync(account.Id, cancellationToken);
            if (hasTransactions)
            {
                account.Archive();
            }

            accountRepository.Delete(account);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return Results.NoContent();
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }

    private static AccountResponse MapToResponse(Account account)
    {
        return new AccountResponse(
            account.Id.Value,
            account.Name.Value,
            account.Type.ToString(),
            account.InitialBalance);
    }
}

/// <summary>
/// Contrato de saída das respostas do módulo de contas.
/// </summary>
public record AccountResponse(Guid Id, string Name, string Type, decimal InitialBalance);

/// <summary>
/// Contrato de entrada para a criação de contas.
/// </summary>
public record CreateAccountRequest(string Name, string Type, decimal InitialBalance);

/// <summary>
/// Contrato de entrada para a atualização de contas.
/// </summary>
public record UpdateAccountRequest(string Name, string Type, decimal InitialBalance);
