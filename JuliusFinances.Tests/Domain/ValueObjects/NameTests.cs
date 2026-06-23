using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class NameTests
{
    [Theory]
    [InlineData("Diogo Silva", "Diogo Silva")]
    [InlineData("diogo silva", "Diogo Silva")]
    [InlineData("DIOGO SILVA", "Diogo Silva")]
    [InlineData("  diogo   silva  ", "Diogo Silva")] // Trim + title case das palavras + normalização de espaços
    [InlineData("diogo da silva e souza", "Diogo da Silva e Souza")] // Preposições e conjunções minúsculas
    [InlineData("MARIA DO CARMO DOS SANTOS", "Maria do Carmo dos Santos")] // Preposições de maiúsculo para minúsculo
    [InlineData("Deodoro da Fonseca", "Deodoro da Fonseca")] // Primeiro nome mantém maiúscula mesmo se for preposição
    public void Constructor_WithValidName_ShouldCreateInstanceAndCapitalize(string input, string expected)
    {
        // Act
        var name = new Name(input);

        // Assert
        Assert.Equal(expected, name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithNullOrEmptyName_ShouldThrowDomainException(string? input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Name(input!));
        Assert.Equal("O nome não pode ser nulo ou vazio.", exception.Message);
    }

    [Theory]
    [InlineData("Ab")] // Curto demais (menos de 3 chars)
    [InlineData("a")]
    public void Constructor_WithTooShortName_ShouldThrowDomainException(string input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Name(input));
        Assert.Equal("O nome deve conter entre 3 e 150 caracteres.", exception.Message);
    }

    [Fact]
    public void Constructor_WithTooLongName_ShouldThrowDomainException()
    {
        // Arrange
        var longName = new string('a', 151);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Name(longName));
        Assert.Equal("O nome deve conter entre 3 e 150 caracteres.", exception.Message);
    }
}
