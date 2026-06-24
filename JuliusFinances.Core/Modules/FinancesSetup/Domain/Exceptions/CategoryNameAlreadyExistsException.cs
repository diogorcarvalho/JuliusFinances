using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.Exceptions;

/// <summary>
/// Exceção lançada quando uma categoria com o mesmo nome já existe para o usuário ou como global.
/// </summary>
public class CategoryNameAlreadyExistsException : DomainException
{
    public CategoryNameAlreadyExistsException(string name)
        : base($"Já existe uma categoria ativa com o nome '{name}'.") { }
}
