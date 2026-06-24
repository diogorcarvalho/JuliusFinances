using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor local do módulo de finanças que encapsula o identificador do usuário proprietário.
/// </summary>
public record OwnerId
{
    public Guid Value { get; }

    public OwnerId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("O identificador do proprietário não pode ser vazio.");
        }
        Value = value;
    }
}
