using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.Auth.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma tentativa de registro utiliza um e-mail que já existe no sistema.
/// </summary>
public class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email)
        : base($"O e-mail '{email}' já está cadastrado no sistema.") { }
}
