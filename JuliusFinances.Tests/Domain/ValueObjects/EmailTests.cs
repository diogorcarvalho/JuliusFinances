using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("usuario@exemplo.com", "usuario@exemplo.com")]
    [InlineData("USUARIO@EXEMPLO.COM", "usuario@exemplo.com")]
    [InlineData("  uSuArIo@eXeMpLo.CoM  ", "usuario@exemplo.com")]
    public void Constructor_WithValidEmail_ShouldCreateInstanceAndNormalize(string input, string expected)
    {
        // Act
        var email = new Email(input);

        // Assert
        Assert.Equal(expected, email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithNullOrEmptyEmail_ShouldThrowDomainException(string? input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Email(input!));
        Assert.Equal("O e-mail não pode ser nulo ou vazio.", exception.Message);
    }

    [Theory]
    [InlineData("usuarioexemplo.com")] // sem @
    [InlineData("usuario@")] // sem domínio
    [InlineData("@exemplo.com")] // sem usuário
    [InlineData("usuario@exemplo")] // sem extensão .com etc.
    public void Constructor_WithInvalidEmailFormat_ShouldThrowDomainException(string input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Email(input));
        Assert.Equal("O e-mail informado está em um formato inválido.", exception.Message);
    }
}
