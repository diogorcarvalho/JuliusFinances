using Microsoft.EntityFrameworkCore;
using JuliusFinances.Core.Modules.Auth.Application.Interfaces;
using JuliusFinances.Core.Modules.Auth.Domain.Entities;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;
using JuliusFinances.Api.Common.Database;

namespace JuliusFinances.Api.Modules.Auth.Infrastructure.Persistence;

/// <summary>
/// Implementação concreta do repositório de usuários utilizando Entity Framework Core.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly JuliusDbContext _dbContext;

    public UserRepository(JuliusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }
}
