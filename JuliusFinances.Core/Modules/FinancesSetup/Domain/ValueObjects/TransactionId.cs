using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula o identificador único da transação (Strongly Typed ID).
/// </summary>
public record TransactionId
{
    public Guid Value { get; }

    public TransactionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("O identificador da transação não pode ser vazio.");
        }
        Value = value;
    }

    public static TransactionId Unique() => new(Guid.NewGuid());
}
