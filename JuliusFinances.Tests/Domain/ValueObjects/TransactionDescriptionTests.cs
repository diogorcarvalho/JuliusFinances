using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class TransactionDescriptionTests
{
    [Fact]
    public void Constructor_WithValidValue_ShouldCreateInstanceAndTrim()
    {
        // Act
        var desc = new TransactionDescription("  Almoço de Negócios  ");

        // Assert
        Assert.Equal("Almoço de Negócios", desc.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyValue_ShouldThrowDomainException(string? value)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new TransactionDescription(value!));
        Assert.Equal("A descrição da transação não pode ser nula ou vazia.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValueTooShort_ShouldThrowDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new TransactionDescription("Oi"));
        Assert.Equal("A descrição da transação deve conter entre 3 e 250 caracteres.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValueTooLong_ShouldThrowDomainException()
    {
        // Arrange
        var longValue = new string('A', 251);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new TransactionDescription(longValue));
        Assert.Equal("A descrição da transação deve conter entre 3 e 250 caracteres.", exception.Message);
    }
}
