using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula o identificador único da transferência (Strongly Typed ID).
/// </summary>
public record TransferId
{
    public Guid Value { get; }

    public TransferId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("O identificador da transferência não pode ser vazio.");
        }
        Value = value;
    }

    public static TransferId Unique() => new(Guid.NewGuid());
}
