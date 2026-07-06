using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula e valida a descrição textual da movimentação.
/// </summary>
public record TransactionDescription
{
    public string Value { get; }

    public TransactionDescription(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("A descrição da transação não pode ser nula ou vazia.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length < 3 || trimmed.Length > 250)
        {
            throw new DomainException("A descrição da transação deve conter entre 3 e 250 caracteres.");
        }

        Value = trimmed;
    }
}
