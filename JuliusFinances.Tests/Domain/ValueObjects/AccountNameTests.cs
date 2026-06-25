using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class AccountNameTests
{
    [Theory]
    [InlineData("Carteira", "Carteira")]
    [InlineData("  carteira itaú  ", "Carteira Itaú")]
    [InlineData("banco    do   brasil", "Banco Do Brasil")]
    [InlineData("poupança", "Poupança")]
    public void Constructor_WithValidValue_ShouldNormalizeSpacesAndCapitalize(string input, string expected)
    {
        // Act
        var name = new AccountName(input);

        // Assert
        Assert.Equal(expected, name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithNullOrEmptyValue_ShouldThrowDomainException(string? input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new AccountName(input!));
        Assert.Equal("O nome da conta não pode ser nulo ou vazio.", exception.Message);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Constructor_WithTooShortValue_ShouldThrowDomainException(string input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new AccountName(input));
        Assert.Equal("O nome da conta deve conter entre 3 e 100 caracteres.", exception.Message);
    }

    [Fact]
    public void Constructor_WithTooLongValue_ShouldThrowDomainException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new AccountName(longName));
        Assert.Equal("O nome da conta deve conter entre 3 e 100 caracteres.", exception.Message);
    }

    [Theory]
    [InlineData("Carteira Itaú", "carteira itau")]
    [InlineData("Poupança", "poupanca")]
    [InlineData("Banco Digital", "banco digital")]
    public void GetNormalizedForComparison_ShouldRemoveAccentsAndConvertToLowercase(string input, string expected)
    {
        // Arrange
        var accountName = new AccountName(input);

        // Act
        var normalized = accountName.GetNormalizedForComparison();

        // Assert
        Assert.Equal(expected, normalized);
    }
}
