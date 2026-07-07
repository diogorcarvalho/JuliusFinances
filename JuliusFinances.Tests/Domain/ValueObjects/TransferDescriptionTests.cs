using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class TransferDescriptionTests
{
    [Fact]
    public void Constructor_WithValidValue_ShouldCreateInstanceAndTrim()
    {
        // Act
        var desc = new TransferDescription("  Aplicação Poupança  ");

        // Assert
        Assert.Equal("Aplicação Poupança", desc.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrEmptyValue_ShouldSetDefaultValue(string? value)
    {
        // Act
        var desc = new TransferDescription(value);

        // Assert
        Assert.Equal("Transferência entre Contas", desc.Value);
    }

    [Fact]
    public void Constructor_WithValueTooShort_ShouldThrowDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new TransferDescription("Oi"));
        Assert.Equal("A descrição da transferência deve conter entre 3 e 250 caracteres.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValueTooLong_ShouldThrowDomainException()
    {
        // Arrange
        var longValue = new string('A', 251);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new TransferDescription(longValue));
        Assert.Equal("A descrição da transferência deve conter entre 3 e 250 caracteres.", exception.Message);
    }
}
