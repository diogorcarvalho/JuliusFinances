using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma conta com o mesmo nome já existe para o usuário.
/// </summary>
public class AccountNameAlreadyExistsException : DomainException
{
    public AccountNameAlreadyExistsException(string name)
        : base($"Já existe uma conta ativa com o nome '{name}'.") { }
}
