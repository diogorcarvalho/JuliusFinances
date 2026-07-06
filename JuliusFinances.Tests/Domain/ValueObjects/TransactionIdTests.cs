using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class TransactionIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateInstance()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var transactionId = new TransactionId(guid);

        // Assert
        Assert.Equal(guid, transactionId.Value);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrowDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new TransactionId(Guid.Empty));
        Assert.Equal("O identificador da transação não pode ser vazio.", exception.Message);
    }

    [Fact]
    public void Unique_ShouldGenerateValidAndUniqueTransactionIds()
    {
        // Act
        var id1 = TransactionId.Unique();
        var id2 = TransactionId.Unique();

        // Assert
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
        Assert.NotEqual(id1, id2);
    }
}
