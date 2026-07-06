using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor composto que encapsula o comportamento financeiro.
/// </summary>
public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount <= 0 || amount > 99999999999.99m)
        {
            throw new DomainException("O valor da transação deve ser maior que zero e menor ou igual a 99.999.999.999,99.");
        }

        if (currency != "BRL")
        {
            throw new DomainException("A moeda da transação deve ser obrigatoriamente BRL.");
        }

        Amount = amount;
        Currency = currency;
    }
}
