using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um usuário tenta acessar, alterar ou excluir uma conta para a qual não possui permissão.
/// </summary>
public class AccountForbiddenAccessException : DomainException
{
    public AccountForbiddenAccessException()
        : base("Você não possui permissão para acessar ou modificar esta conta.") { }
}
