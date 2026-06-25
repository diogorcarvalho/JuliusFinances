using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class AccountIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateInstance()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var accountId = new AccountId(guid);

        // Assert
        Assert.Equal(guid, accountId.Value);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrowDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new AccountId(Guid.Empty));
        Assert.Equal("O identificador da conta não pode ser vazio.", exception.Message);
    }

    [Fact]
    public void Unique_ShouldGenerateValidAndUniqueAccountIds()
    {
        // Act
        var id1 = AccountId.Unique();
        var id2 = AccountId.Unique();

        // Assert
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
        Assert.NotEqual(id1, id2);
    }
}
