using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class TransferIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateInstance()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var transferId = new TransferId(guid);

        // Assert
        Assert.Equal(guid, transferId.Value);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrowDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new TransferId(Guid.Empty));
        Assert.Equal("O identificador da transferência não pode ser vazio.", exception.Message);
    }

    [Fact]
    public void Unique_ShouldGenerateValidAndUniqueTransferIds()
    {
        // Act
        var id1 = TransferId.Unique();
        var id2 = TransferId.Unique();

        // Assert
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
        Assert.NotEqual(id1, id2);
    }
}
