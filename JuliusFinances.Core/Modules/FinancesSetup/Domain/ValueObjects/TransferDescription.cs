using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula e valida a descrição textual opcional da transferência.
/// </summary>
public record TransferDescription
{
    public string Value { get; }

    public TransferDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Value = "Transferência entre Contas";
            return;
        }

        var trimmed = value.Trim();

        if (trimmed.Length < 3 || trimmed.Length > 250)
        {
            throw new DomainException("A descrição da transferência deve conter entre 3 e 250 caracteres.");
        }

        Value = trimmed;
    }
}
