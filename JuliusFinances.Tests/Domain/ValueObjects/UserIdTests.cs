using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class UserIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateInstance()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var userId = new UserId(guid);

        // Assert
        Assert.Equal(guid, userId.Value);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrowDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new UserId(Guid.Empty));
        Assert.Equal("O identificador do usuário não pode ser vazio.", exception.Message);
    }

    [Fact]
    public void Unique_ShouldGenerateValidAndUniqueUserIds()
    {
        // Act
        var id1 = UserId.Unique();
        var id2 = UserId.Unique();

        // Assert
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
        Assert.NotEqual(id1, id2);
    }
}
