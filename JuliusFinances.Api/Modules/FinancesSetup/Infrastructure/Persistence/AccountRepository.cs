using Microsoft.EntityFrameworkCore;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;
using JuliusFinances.Api.Common.Database;

namespace JuliusFinances.Api.Modules.FinancesSetup.Infrastructure.Persistence;

/// <summary>
/// Implementação concreta do repositório de contas utilizando Entity Framework Core.
/// </summary>
public class AccountRepository : IAccountRepository
{
    private readonly JuliusDbContext _dbContext;

    public AccountRepository(JuliusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var ownerId = new OwnerId(userId);
        return await _dbContext.Accounts
            .Where(a => a.OwnerId == ownerId && !a.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(AccountName name, Guid userId, CancellationToken cancellationToken = default)
    {
        var normalizedInput = name.GetNormalizedForComparison();
        var ownerId = new OwnerId(userId);

        var accounts = await _dbContext.Accounts
            .Where(a => a.OwnerId == ownerId && !a.IsDeleted)
            .ToListAsync(cancellationToken);

        return accounts.Any(a => a.Name.GetNormalizedForComparison() == normalizedInput);
    }

    public async Task<bool> HasLinkedTransactionsAsync(AccountId id, CancellationToken cancellationToken = default)
    {
        var hasTransactions = await _dbContext.Transactions.AnyAsync(t => t.AccountId == id && !t.IsDeleted, cancellationToken);
        if (hasTransactions) return true;

        return await _dbContext.Transfers.AnyAsync(t => (t.OriginAccountId == id || t.DestinationAccountId == id) && !t.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _dbContext.Accounts.AddAsync(account, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Update(Account account)
    {
        _dbContext.Accounts.Update(account);
        _dbContext.SaveChanges();
    }

    public void Delete(Account account)
    {
        if (account.IsDeleted)
        {
            _dbContext.Accounts.Update(account);
        }
        else
        {
            _dbContext.Accounts.Remove(account);
        }
        _dbContext.SaveChanges();
    }
}
