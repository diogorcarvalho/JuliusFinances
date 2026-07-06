using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateInstance()
    {
        // Act
        var money = new Money(150.75m, "BRL");

        // Assert
        Assert.Equal(150.75m, money.Amount);
        Assert.Equal("BRL", money.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(100000000000.00)]
    public void Constructor_WithInvalidAmount_ShouldThrowDomainException(decimal amount)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Money(amount, "BRL"));
        Assert.Equal("O valor da transação deve ser maior que zero e menor ou igual a 99.999.999.999,99.", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidCurrency_ShouldThrowDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Money(50.00m, "USD"));
        Assert.Equal("A moeda da transação deve ser obrigatoriamente BRL.", exception.Message);
    }
}
