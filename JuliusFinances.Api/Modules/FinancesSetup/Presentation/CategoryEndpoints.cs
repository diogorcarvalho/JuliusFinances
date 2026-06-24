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
/// Define os endpoints de rotas do módulo de Categorias agrupados em /categories.
/// </summary>
public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/categories")
                       .RequireAuthorization()
                       .WithTags("Categories");

        group.MapGet("/", ListAsync)
             .WithName("ListCategories")
             .WithSummary("Listar categorias acessíveis")
             .WithDescription("Retorna uma lista contendo as categorias pessoais do usuário autenticado e as categorias globais do sistema.")
             .Produces<IEnumerable<CategoryResponse>>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", GetByIdAsync)
             .WithName("GetCategoryById")
             .WithSummary("Obter categoria por ID")
             .WithDescription("Retorna os detalhes de uma categoria específica, validando se o usuário possui permissão de leitura.")
             .Produces<CategoryResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
             .WithName("CreateCategory")
             .WithSummary("Criar categoria pessoal")
             .WithDescription("Cria uma nova categoria pessoal associada ao usuário autenticado, validando as regras de duplicidade de nome.")
             .Produces<CategoryResponse>(StatusCodes.Status201Created)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateAsync)
             .WithName("UpdateCategory")
             .WithSummary("Atualizar categoria pessoal")
             .WithDescription("Atualiza o nome ou tipo de fluxo de uma categoria pessoal de propriedade do usuário autenticado.")
             .Produces<CategoryResponse>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteAsync)
             .WithName("DeleteCategory")
             .WithSummary("Excluir categoria pessoal")
             .WithDescription("Exclui uma categoria pessoal do usuário autenticado de forma segura e idempotente (Soft Delete).")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ICategoryRepository categoryRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var categories = await categoryRepository.GetByUserIdAndGlobalAsync(userId.Value, cancellationToken);
        var response = categories.Select(MapToResponse);

        return Results.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ICategoryRepository categoryRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var category = await categoryRepository.GetByIdAsync(new CategoryId(id), cancellationToken);
        if (category == null) return Results.NotFound();

        // Valida se a categoria pertence ao usuário autenticado ou é global
        if (category.OwnerId != null && category.OwnerId.Value != userId.Value)
        {
            throw new CategoryForbiddenAccessException();
        }

        return Results.Ok(MapToResponse(category));
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateCategoryRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ICategoryRepository categoryRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        if (!Enum.TryParse<FlowType>(request.FlowType, true, out var flowType))
        {
            throw new DomainException("Tipo de fluxo inválido. Valores permitidos: Income, Expense, Both.");
        }

        var categoryName = new CategoryName(request.Name);

        // Valida se já existe uma categoria ativa com o mesmo nome para o escopo do usuário ou global
        var exists = await categoryRepository.ExistsByNameAsync(categoryName, userId.Value, cancellationToken);
        if (exists)
        {
            throw new CategoryNameAlreadyExistsException(categoryName.Value);
        }

        var category = new Category(
            CategoryId.Unique(),
            categoryName,
            flowType,
            new OwnerId(userId.Value));

        await categoryRepository.AddAsync(category, cancellationToken);

        var response = MapToResponse(category);
        return Results.Created($"/categories/{category.Id.Value}", response);
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequest request,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ICategoryRepository categoryRepository,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var category = await categoryRepository.GetByIdAsync(new CategoryId(id), cancellationToken);
        if (category == null) return Results.NotFound();

        // Usuário só pode editar categorias que são dele (não globais e nem de outros usuários)
        if (category.OwnerId == null || category.OwnerId.Value != userId.Value)
        {
            throw new CategoryForbiddenAccessException();
        }

        if (!Enum.TryParse<FlowType>(request.FlowType, true, out var flowType))
        {
            throw new DomainException("Tipo de fluxo inválido. Valores permitidos: Income, Expense, Both.");
        }

        var newName = new CategoryName(request.Name);

        // Se o nome foi alterado, valida duplicidades no escopo do usuário ou globais (ignorando a própria categoria atual)
        if (category.Name.GetNormalizedForComparison() != newName.GetNormalizedForComparison())
        {
            var exists = await categoryRepository.ExistsByNameAsync(newName, userId.Value, cancellationToken);
            if (exists)
            {
                throw new CategoryNameAlreadyExistsException(newName.Value);
            }
        }

        category.Update(newName, flowType);
        categoryRepository.Update(category);

        return Results.Ok(MapToResponse(category));
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] Guid id,
        ClaimsPrincipal claimsPrincipal,
        [FromServices] ICategoryRepository categoryRepository,
        [FromServices] JuliusDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(claimsPrincipal);
        if (userId == null) return Results.Unauthorized();

        var category = await categoryRepository.GetByIdAsync(new CategoryId(id), cancellationToken);
        if (category == null) return Results.NotFound();

        // Usuário só pode excluir categorias de sua propriedade
        if (category.OwnerId == null || category.OwnerId.Value != userId.Value)
        {
            throw new CategoryForbiddenAccessException();
        }

        // Executa a validação de integridade dentro de uma transação isolada contra condições de corrida
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var hasTransactions = await categoryRepository.HasLinkedTransactionsAsync(category.Id, cancellationToken);
            if (hasTransactions)
            {
                throw new DomainException("Não é possível excluir uma categoria que possui transações vinculadas.");
            }

            categoryRepository.Delete(category);
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

    private static CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse(
            category.Id.Value,
            category.Name.Value,
            category.FlowType.ToString(),
            category.OwnerId == null);
    }
}

/// <summary>
/// Contrato de saída das respostas do módulo de categorias.
/// </summary>
public record CategoryResponse(Guid Id, string Name, string FlowType, bool IsGlobal);

/// <summary>
/// Contrato de entrada para a criação de categorias.
/// </summary>
public record CreateCategoryRequest(string Name, string FlowType);

/// <summary>
/// Contrato de entrada para a atualização de categorias.
/// </summary>
public record UpdateCategoryRequest(string Name, string FlowType);
