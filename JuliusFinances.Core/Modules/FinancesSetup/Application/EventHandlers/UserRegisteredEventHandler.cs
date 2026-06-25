using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.Auth.Domain.Events;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Repositories;

namespace JuliusFinances.Core.Modules.FinancesSetup.Application.EventHandlers;

/// <summary>
/// Manipulador que escuta a criação de novos usuários e cria de forma autônoma a conta pessoal padrão do onboarding.
/// </summary>
public class UserRegisteredEventHandler : IDomainEventHandler<UserRegisteredEvent>
{
    private readonly IAccountRepository _accountRepository;

    public UserRegisteredEventHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var account = new Account(
            AccountId.Unique(),
            new AccountName("Carteira"),
            AccountType.Cash,
            0.00m,
            new OwnerId(domainEvent.UserId));

        await _accountRepository.AddAsync(account, cancellationToken);
    }
}
