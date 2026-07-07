using Microsoft.EntityFrameworkCore;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;
using JuliusFinances.Api.Common.Database;

namespace JuliusFinances.Api.Modules.FinancesSetup.Infrastructure.Persistence;

/// <summary>
/// Implementação concreta do repositório de transferências utilizando Entity Framework Core.
/// </summary>
public class TransferRepository : ITransferRepository
{
    private readonly JuliusDbContext _dbContext;

    public TransferRepository(JuliusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Transfer?> GetByIdAsync(TransferId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transfers
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Transfer>> GetPagedByUserIdAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var ownerId = new OwnerId(userId);
        return await _dbContext.Transfers
            .Where(t => t.OwnerId == ownerId)
            .OrderByDescending(t => t.TransferDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transfer transfer, CancellationToken cancellationToken = default)
    {
        await _dbContext.Transfers.AddAsync(transfer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void Update(Transfer transfer)
    {
        _dbContext.Transfers.Update(transfer);
        _dbContext.SaveChanges();
    }

    public void Delete(Transfer transfer)
    {
        transfer.Archive();
        _dbContext.Transfers.Update(transfer);
        _dbContext.SaveChanges();
    }
}
