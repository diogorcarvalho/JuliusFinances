using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula o identificador único da conta (Strongly Typed ID).
/// </summary>
public record AccountId
{
    public Guid Value { get; }

    public AccountId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("O identificador da conta não pode ser vazio.");
        }
        Value = value;
    }

    public static AccountId Unique() => new(Guid.NewGuid());
}
