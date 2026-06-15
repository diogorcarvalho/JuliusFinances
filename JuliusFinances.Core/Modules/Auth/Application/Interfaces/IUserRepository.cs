using JuliusFinances.Core.Modules.Auth.Domain.Entities;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.Auth.Application.Interfaces;

/// <summary>
/// Contrato para persistência e consulta de usuários.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
}
