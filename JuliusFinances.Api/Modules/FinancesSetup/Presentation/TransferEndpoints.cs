using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Exceptions;
using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Api.Modules.FinancesSetup.Presentation;

/// <summary>
/// Define os endpoints de rotas do módulo de Transferências agrupados em /transfers.
/// </summary>
public static class TransferEndpoints
{
    private static readonly CategoryId TransferCategoryId = new(Guid.Parse("de250014-c812-4c22-9014-99859f123456"));

    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/transfers")
                       .RequireAuthorization()
                       .WithTags("Transfers");

        group.MapGet("/", ListAsync)
             .WithName("ListTransfers")
             .WithSummary("Listar transferências paginadas")
             .WithDescription("Retorna as movimentações de transferência ativas do usuário autenticado de forma paginada.")
             .Produces<IEnumerable<TransferResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetByIdAsync)
             .WithName("GetTransferById")
             .WithSummary("Obter transferência por ID")
             .WithDescription("Retorna os detalhes de uma transferência específica pertencente ao usuário autenticado.")
             .Produces<TransferResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
             .WithName("CreateTransfer")
             .WithSummary("Criar transferência")
             .WithDescription("Registra uma nova transferência entre duas contas distintas do usuário autenticado.")
             .Produces<TransferResponse>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateAsync)
             .WithName("UpdateTransfer")
             .WithSummary("Atualizar transferência")
             .WithDescription("Atualiza os dados de uma transferência ativa pertencente ao usuário autenticado.")
             .Produces<TransferResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
             .WithName("DeleteTransfer")
             .WithSummary("Excluir/Arquivar transferência")
             .WithDescription("Arquiva logicamente uma transferência pertencente ao usuário autenticado.")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransferRepository transferRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var resolvedPage = page.HasValue && page.Value >= 1 ? page.Value : 1;
        var resolvedPageSize = pageSize.HasValue && pageSize.Value >= 1 && pageSize.Value <= 100 ? pageSize.Value : 20;

        var transfers = await transferRepository.GetPagedByUserIdAsync(
            userId.Value,
            resolvedPage,
            resolvedPageSize,
            cancellationToken);

        var response = transfers.Select(MapToResponse);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransferRepository transferRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var transfer = await transferRepository.GetByIdAsync(new TransferId(id), cancellationToken);
        if (transfer == null || transfer.IsDeleted) return Results.NotFound();

        if (transfer.OwnerId.Value != userId.Value)
        {
            throw new TransferForbiddenAccessException();
        }

        return Results.Ok(MapToResponse(transfer));
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateTransferRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransferRepository transferRepository,
        [FromServices] IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        if (request.OriginAccountId == request.DestinationAccountId)
        {
            throw new DomainException("A conta de origem e a conta de destino devem ser obrigatoriamente diferentes.");
        }

        if (request.Currency != "BRL")
        {
            throw new DomainException("A moeda da transação deve ser obrigatoriamente BRL.");
        }

        // Verifica Conta de Origem
        var originAccount = await accountRepository.GetByIdAsync(new AccountId(request.OriginAccountId), cancellationToken);
        if (originAccount == null) return Results.NotFound();
        if (originAccount.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }
        if (originAccount.IsDeleted)
        {
            throw new DomainException("Não é possível associar transferências a uma conta arquivada.");
        }

        // Verifica Conta de Destino
        var destinationAccount = await accountRepository.GetByIdAsync(new AccountId(request.DestinationAccountId), cancellationToken);
        if (destinationAccount == null) return Results.NotFound();
        if (destinationAccount.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }
        if (destinationAccount.IsDeleted)
        {
            throw new DomainException("Não é possível associar transferências a uma conta arquivada.");
        }

        var desc = new TransferDescription(request.Description);
        var money = new Money(request.Amount, request.Currency);
        var transferId = TransferId.Unique();

        var transfer = new Transfer(
            transferId,
            desc,
            money,
            originAccount.Id,
            destinationAccount.Id,
            TransferCategoryId,
            new OwnerId(userId.Value),
            request.TransferDate);

        await transferRepository.AddAsync(transfer, cancellationToken);

        var response = MapToResponse(transfer);
        return Results.Created($"/transfers/{transfer.Id.Value}", response);
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateTransferRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransferRepository transferRepository,
        [FromServices] IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        if (request.OriginAccountId == request.DestinationAccountId)
        {
            throw new DomainException("A conta de origem e a conta de destino devem ser obrigatoriamente diferentes.");
        }

        var transfer = await transferRepository.GetByIdAsync(new TransferId(id), cancellationToken);
        if (transfer == null || transfer.IsDeleted) return Results.NotFound();

        if (transfer.OwnerId.Value != userId.Value)
        {
            throw new TransferForbiddenAccessException();
        }

        if (request.Currency != "BRL")
        {
            throw new DomainException("A moeda da transação deve ser obrigatoriamente BRL.");
        }

        // Verifica Conta de Origem
        var originAccount = await accountRepository.GetByIdAsync(new AccountId(request.OriginAccountId), cancellationToken);
        if (originAccount == null) return Results.NotFound();
        if (originAccount.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }
        if (originAccount.IsDeleted)
        {
            throw new DomainException("Não é possível associar transferências a uma conta arquivada.");
        }

        // Verifica Conta de Destino
        var destinationAccount = await accountRepository.GetByIdAsync(new AccountId(request.DestinationAccountId), cancellationToken);
        if (destinationAccount == null) return Results.NotFound();
        if (destinationAccount.OwnerId.Value != userId.Value)
        {
            throw new AccountForbiddenAccessException();
        }
        if (destinationAccount.IsDeleted)
        {
            throw new DomainException("Não é possível associar transferências a uma conta arquivada.");
        }

        var desc = new TransferDescription(request.Description);
        var money = new Money(request.Amount, request.Currency);

        transfer.Update(
            desc,
            money,
            originAccount.Id,
            destinationAccount.Id,
            request.TransferDate);

        transferRepository.Update(transfer);

        return Results.Ok(MapToResponse(transfer));
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ITransferRepository transferRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var transfer = await transferRepository.GetByIdAsync(new TransferId(id), cancellationToken);
        if (transfer == null || transfer.IsDeleted) return Results.NotFound();

        if (transfer.OwnerId.Value != userId.Value)
        {
            throw new TransferForbiddenAccessException();
        }

        transferRepository.Delete(transfer);

        return Results.NoContent();
    }

    private static Guid? GetUserId(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst("sub");
        return claim != null && Guid.TryParse(claim.Value, out var userId) ? userId : null;
    }

    private static TransferResponse MapToResponse(Transfer transfer)
    {
        return new TransferResponse(
            transfer.Id.Value,
            transfer.Description.Value,
            new MoneyResponse(transfer.Money.Amount, transfer.Money.Currency),
            transfer.OriginAccountId.Value,
            transfer.DestinationAccountId.Value,
            transfer.CategoryId.Value,
            transfer.TransferDate);
    }
}

/// <summary>
/// Contrato de saída das respostas do módulo de transferências.
/// </summary>
public record TransferResponse(
    Guid Id,
    string Description,
    MoneyResponse Money,
    Guid OriginAccountId,
    Guid DestinationAccountId,
    Guid CategoryId,
    DateTime TransferDate);

/// <summary>
/// Contrato de entrada para a criação de transferências.
/// </summary>
public record CreateTransferRequest(
    string? Description,
    decimal Amount,
    string Currency,
    Guid OriginAccountId,
    Guid DestinationAccountId,
    DateTime TransferDate);

/// <summary>
/// Contrato de entrada para a atualização de transferências.
/// </summary>
public record UpdateTransferRequest(
    string? Description,
    decimal Amount,
    string Currency,
    Guid OriginAccountId,
    Guid DestinationAccountId,
    DateTime TransferDate);
