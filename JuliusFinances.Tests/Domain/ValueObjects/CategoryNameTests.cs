using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class CategoryNameTests
{
    [Theory]
    [InlineData("Pet", "Pet")]
    [InlineData("  alimentação  ", "Alimentação")]
    [InlineData("lazer    &   entretenimento", "Lazer & entretenimento")]
    [InlineData("combustível", "Combustível")]
    public void Constructor_WithValidValue_ShouldNormalizeSpacesAndCapitalize(string input, string expected)
    {
        // Act
        var name = new CategoryName(input);

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
        var exception = Assert.Throws<DomainException>(() => new CategoryName(input!));
        Assert.Equal("O nome da categoria não pode ser nulo ou vazio.", exception.Message);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Constructor_WithTooShortValue_ShouldThrowDomainException(string input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new CategoryName(input));
        Assert.Equal("O nome da categoria deve conter entre 3 e 100 caracteres.", exception.Message);
    }

    [Fact]
    public void Constructor_WithTooLongValue_ShouldThrowDomainException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new CategoryName(longName));
        Assert.Equal("O nome da categoria deve conter entre 3 e 100 caracteres.", exception.Message);
    }

    [Theory]
    [InlineData("Alimentação", "alimentacao")]
    [InlineData("Habitação", "habitacao")]
    [InlineData("Saúdé", "saude")]
    [InlineData("Lazer & Entretenimento", "lazer & entretenimento")]
    public void GetNormalizedForComparison_ShouldRemoveAccentsAndConvertToLowercase(string input, string expected)
    {
        // Arrange
        var categoryName = new CategoryName(input);

        // Act
        var normalized = categoryName.GetNormalizedForComparison();

        // Assert
        Assert.Equal(expected, normalized);
    }
}
