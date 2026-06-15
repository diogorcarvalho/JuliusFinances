using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Common.Security;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class PasswordTests
{
    private class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string plainTextPassword) => $"hashed_{plainTextPassword}";
        public bool Verify(string plainTextPassword, string hashedPassword) => hashedPassword == $"hashed_{plainTextPassword}";
    }

    [Fact]
    public void Constructor_WithValidHash_ShouldCreateInstanceDirectly()
    {
        // Act
        var password = new Password("some_hashed_value");

        // Assert
        Assert.Equal("some_hashed_value", password.HashValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyHash_ShouldThrowDomainException(string? input)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new Password(input!));
    }

    [Theory]
    [InlineData("SenhaSegura123!")]
    [InlineData("Abcde12@")]
    public void Create_WithValidPassword_ShouldCreateHashedInstance(string plainText)
    {
        // Arrange
        var hasher = new FakePasswordHasher();

        // Act
        var password = Password.Create(plainText, hasher);

        // Assert
        Assert.Equal($"hashed_{plainText}", password.HashValue);
    }

    [Theory]
    [InlineData("senha")] // Curto demais e sem critérios
    [InlineData("senhasegura123!")] // Sem maiúscula
    [InlineData("SENHASEGURA123!")] // Sem minúscula
    [InlineData("SenhaSegura!")] // Sem número
    [InlineData("SenhaSegura123")] // Sem caractere especial
    public void Create_WithWeakPassword_ShouldThrowDomainException(string plainText)
    {
        // Arrange
        var hasher = new FakePasswordHasher();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => Password.Create(plainText, hasher));
        Assert.Equal("A senha deve conter no mínimo 8 caracteres, pelo menos uma letra maiúscula, uma minúscula, um número e um caractere especial.", exception.Message);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var hasher = new FakePasswordHasher();
        var password = Password.Create("SenhaSegura123!", hasher);

        // Act
        var result = password.Verify("SenhaSegura123!", hasher);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var hasher = new FakePasswordHasher();
        var password = Password.Create("SenhaSegura123!", hasher);

        // Act
        var result = password.Verify("SenhaIncorreta123!", hasher);

        // Assert
        Assert.False(result);
    }
}
