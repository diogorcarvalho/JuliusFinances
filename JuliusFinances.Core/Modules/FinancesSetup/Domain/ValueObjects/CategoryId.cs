using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula o identificador único da categoria (Strongly Typed ID).
/// </summary>
public record CategoryId
{
    public Guid Value { get; }

    public CategoryId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("O identificador da categoria não pode ser vazio.");
        }
        Value = value;
    }

    public static CategoryId Unique() => new(Guid.NewGuid());
}
