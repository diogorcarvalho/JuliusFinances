using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando um usuário tenta acessar, alterar ou excluir uma transação para a qual não possui permissão.
/// </summary>
public class TransactionForbiddenAccessException : DomainException
{
    public TransactionForbiddenAccessException()
        : base("Você não possui permissão para acessar ou modificar esta transação.") { }
}
