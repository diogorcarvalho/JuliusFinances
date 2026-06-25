using JuliusFinances.Core.Modules.Auth.Domain.Events;
using JuliusFinances.Core.Modules.FinancesSetup.Application.EventHandlers;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;
using System.Collections.Concurrent;

namespace JuliusFinances.Tests.EventHandlers;

public class UserRegisteredEventHandlerTests
{
    private class InMemoryAccountRepository : IAccountRepository
    {
        public readonly ConcurrentDictionary<Guid, Account> Accounts = new();

        public Task<Account?> GetByIdAsync(AccountId id, CancellationToken cancellationToken = default)
        {
            Accounts.TryGetValue(id.Value, out var account);
            return Task.FromResult(account);
        }

        public Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var ownerId = new OwnerId(userId);
            var result = Accounts.Values.Where(a => a.OwnerId == ownerId && !a.IsDeleted);
            return Task.FromResult(result);
        }

        public Task<bool> ExistsByNameAsync(AccountName name, Guid userId, CancellationToken cancellationToken = default)
        {
            var ownerId = new OwnerId(userId);
            var normalizedInput = name.GetNormalizedForComparison();
            var exists = Accounts.Values.Any(a => a.OwnerId == ownerId && !a.IsDeleted && a.Name.GetNormalizedForComparison() == normalizedInput);
            return Task.FromResult(exists);
        }

        public Task<bool> HasLinkedTransactionsAsync(AccountId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            Accounts[account.Id.Value] = account;
            return Task.CompletedTask;
        }

        public void Update(Account account)
        {
            Accounts[account.Id.Value] = account;
        }

        public void Delete(Account account)
        {
            if (account.IsDeleted)
            {
                Accounts[account.Id.Value] = account;
            }
            else
            {
                Accounts.TryRemove(account.Id.Value, out _);
            }
        }
    }

    [Fact]
    public async Task HandleAsync_WhenUserIsRegistered_ShouldCreateDefaultCashAccount()
    {
        // Arrange
        var repository = new InMemoryAccountRepository();
        var handler = new UserRegisteredEventHandler(repository);
        var userId = Guid.NewGuid();
        var domainEvent = new UserRegisteredEvent(userId);

        // Act
        await handler.HandleAsync(domainEvent);

        // Assert
        Assert.Single(repository.Accounts);
        var createdAccount = repository.Accounts.Values.First();
        Assert.Equal("Carteira", createdAccount.Name.Value);
        Assert.Equal(AccountType.Cash, createdAccount.Type);
        Assert.Equal(0.00m, createdAccount.InitialBalance);
        Assert.Equal(userId, createdAccount.OwnerId.Value);
        Assert.False(createdAccount.IsDeleted);
    }
}
