using Microsoft.EntityFrameworkCore;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;
using JuliusFinances.Api.Common.Database;

namespace JuliusFinances.Api.Modules.FinancesSetup.Infrastructure.Persistence;

/// <summary>
/// Implementação concreta do repositório de transações utilizando Entity Framework Core.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly JuliusDbContext _dbContext;

    public TransactionRepository(JuliusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transaction?> GetByIdAsync(TransactionId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? accountId = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = new OwnerId(userId);
        var query = _dbContext.Transactions
            .Where(t => t.OwnerId == ownerId);

        if (accountId.HasValue)
        {
            var accId = new AccountId(accountId.Value);
            query = query.Where(t => t.AccountId == accId);
        }

        if (categoryId.HasValue)
        {
            var catId = new CategoryId(categoryId.Value);
            query = query.Where(t => t.CategoryId == catId);
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Update(Transaction transaction)
    {
        _dbContext.Transactions.Update(transaction);
        _dbContext.SaveChanges();
    }

    public void Delete(Transaction transaction)
    {
        transaction.Archive();
        _dbContext.Transactions.Update(transaction);
        _dbContext.SaveChanges();
    }
}
