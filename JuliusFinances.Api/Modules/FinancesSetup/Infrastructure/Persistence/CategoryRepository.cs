using Microsoft.EntityFrameworkCore;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;
using JuliusFinances.Api.Common.Database;

namespace JuliusFinances.Api.Modules.FinancesSetup.Infrastructure.Persistence;

/// <summary>
/// Implementação concreta do repositório de categorias utilizando Entity Framework Core.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly JuliusDbContext _dbContext;

    public CategoryRepository(JuliusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetByUserIdAndGlobalAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var ownerId = new OwnerId(userId);
        return await _dbContext.Categories
            .Where(c => c.OwnerId == null || c.OwnerId == ownerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(CategoryName name, Guid? userId, CancellationToken cancellationToken = default)
    {
        var normalizedInput = name.GetNormalizedForComparison();

        // Recupera todas as categorias ativas (globais e do usuário) para comparar em memória
        // garantindo remoção de acentos e capitalização consistente
        var categories = await _dbContext.Categories
            .Where(c => c.OwnerId == null || (userId.HasValue && c.OwnerId == new OwnerId(userId.Value)))
            .ToListAsync(cancellationToken);

        return categories.Any(c => c.Name.GetNormalizedForComparison() == normalizedInput);
    }

    public async Task<bool> HasLinkedTransactionsAsync(CategoryId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions.AnyAsync(t => t.CategoryId == id && !t.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Update(Category category)
    {
        _dbContext.Categories.Update(category);
        _dbContext.SaveChanges();
    }

    public void Delete(Category category)
    {
        // Marca como arquivada (Soft Delete)
        category.Archive();
        _dbContext.Categories.Update(category);
        _dbContext.SaveChanges();
    }
}
