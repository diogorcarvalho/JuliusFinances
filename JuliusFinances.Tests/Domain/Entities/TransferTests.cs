using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.Entities;

public class TransferTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateTransfer()
    {
        // Arrange
        var id = TransferId.Unique();
        var description = new TransferDescription("Doc para Poupança");
        var money = new Money(150.00m, "BRL");
        var originAccountId = AccountId.Unique();
        var destinationAccountId = AccountId.Unique();
        var categoryId = CategoryId.Unique();
        var ownerId = new OwnerId(Guid.NewGuid());
        var date = DateTime.UtcNow;

        // Act
        var transfer = new Transfer(
            id,
            description,
            money,
            originAccountId,
            destinationAccountId,
            categoryId,
            ownerId,
            date);

        // Assert
        Assert.Equal(id, transfer.Id);
        Assert.Equal(description, transfer.Description);
        Assert.Equal(money, transfer.Money);
        Assert.Equal(originAccountId, transfer.OriginAccountId);
        Assert.Equal(destinationAccountId, transfer.DestinationAccountId);
        Assert.Equal(categoryId, transfer.CategoryId);
        Assert.Equal(ownerId, transfer.OwnerId);
        Assert.Equal(DateTimeKind.Utc, transfer.TransferDate.Kind);
        Assert.True((DateTime.UtcNow - transfer.CreatedAt).TotalSeconds < 5);
        Assert.Null(transfer.UpdatedAt);
        Assert.False(transfer.IsDeleted);
    }

    [Fact]
    public void Constructor_WithSameOriginAndDestinationAccount_ShouldThrowDomainException()
    {
        // Arrange
        var accountId = AccountId.Unique();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Transfer(
            TransferId.Unique(),
            new TransferDescription("Inválido"),
            new Money(100.00m, "BRL"),
            accountId,
            accountId,
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            DateTime.UtcNow));

        Assert.Equal("A conta de origem e a conta de destino devem ser obrigatoriamente diferentes.", exception.Message);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Constructor_WithDateOutsideAllowedRange_ShouldThrowDomainException(int invalidYear)
    {
        // Arrange
        var invalidDate = new DateTime(invalidYear, 1, 1);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Transfer(
            TransferId.Unique(),
            new TransferDescription("Inválido"),
            new Money(100.00m, "BRL"),
            AccountId.Unique(),
            AccountId.Unique(),
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            invalidDate));

        Assert.Equal("O ano da data da transferência deve estar obrigatoriamente compreendido entre o ano 2000 e o ano 2100.", exception.Message);
    }

    [Fact]
    public void Update_WithValidArguments_ShouldModifyPropertiesAndSetUpdatedAt()
    {
        // Arrange
        var transfer = new Transfer(
            TransferId.Unique(),
            new TransferDescription("Doc para Poupança"),
            new Money(150.00m, "BRL"),
            AccountId.Unique(),
            AccountId.Unique(),
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            DateTime.UtcNow);

        var newDesc = new TransferDescription("Resgate");
        var newMoney = new Money(200.00m, "BRL");
        var newOrigin = AccountId.Unique();
        var newDestination = AccountId.Unique();
        var newDate = DateTime.UtcNow.AddDays(1);

        // Act
        transfer.Update(newDesc, newMoney, newOrigin, newDestination, newDate);

        // Assert
        Assert.Equal(newDesc, transfer.Description);
        Assert.Equal(newMoney, transfer.Money);
        Assert.Equal(newOrigin, transfer.OriginAccountId);
        Assert.Equal(newDestination, transfer.DestinationAccountId);
        Assert.Equal(DateTimeKind.Utc, transfer.TransferDate.Kind);
        Assert.NotNull(transfer.UpdatedAt);
        Assert.True((DateTime.UtcNow - transfer.UpdatedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public void Update_WithSameOriginAndDestinationAccount_ShouldThrowDomainException()
    {
        // Arrange
        var transfer = new Transfer(
            TransferId.Unique(),
            new TransferDescription("Doc para Poupança"),
            new Money(150.00m, "BRL"),
            AccountId.Unique(),
            AccountId.Unique(),
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            DateTime.UtcNow);

        var sameAccount = AccountId.Unique();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            transfer.Update(
                transfer.Description,
                transfer.Money,
                sameAccount,
                sameAccount,
                transfer.TransferDate));

        Assert.Equal("A conta de origem e a conta de destino devem ser obrigatoriamente diferentes.", exception.Message);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Update_WithDateOutsideAllowedRange_ShouldThrowDomainException(int invalidYear)
    {
        // Arrange
        var transfer = new Transfer(
            TransferId.Unique(),
            new TransferDescription("Doc para Poupança"),
            new Money(150.00m, "BRL"),
            AccountId.Unique(),
            AccountId.Unique(),
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            DateTime.UtcNow);

        var invalidDate = new DateTime(invalidYear, 1, 1);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            transfer.Update(
                transfer.Description,
                transfer.Money,
                transfer.OriginAccountId,
                transfer.DestinationAccountId,
                invalidDate));

        Assert.Equal("O ano da data da transferência deve estar obrigatoriamente compreendido entre o ano 2000 e o ano 2100.", exception.Message);
    }

    [Fact]
    public void Archive_ShouldSetIsDeletedToTrueAndModifyUpdatedAt()
    {
        // Arrange
        var transfer = new Transfer(
            TransferId.Unique(),
            new TransferDescription("Doc para Poupança"),
            new Money(150.00m, "BRL"),
            AccountId.Unique(),
            AccountId.Unique(),
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            DateTime.UtcNow);

        // Act
        transfer.Archive();

        // Assert
        Assert.True(transfer.IsDeleted);
        Assert.NotNull(transfer.UpdatedAt);
        Assert.True((DateTime.UtcNow - transfer.UpdatedAt.Value).TotalSeconds < 5);
    }
}
