using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;

namespace JuliusFinances.Tests.Domain.Entities;

public class TransactionTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateTransaction()
    {
        // Arrange
        var id = TransactionId.Unique();
        var description = new TransactionDescription("Supermercado Compre Bem");
        var type = TransactionType.Expense;
        var money = new Money(250.50m, "BRL");
        var accountId = AccountId.Unique();
        var categoryId = CategoryId.Unique();
        var ownerId = new OwnerId(Guid.NewGuid());
        var date = DateTime.UtcNow;

        // Act
        var transaction = new Transaction(
            id,
            description,
            type,
            money,
            accountId,
            categoryId,
            ownerId,
            date,
            FlowType.Expense);

        // Assert
        Assert.Equal(id, transaction.Id);
        Assert.Equal(description, transaction.Description);
        Assert.Equal(type, transaction.Type);
        Assert.Equal(money, transaction.Money);
        Assert.Equal(accountId, transaction.AccountId);
        Assert.Equal(categoryId, transaction.CategoryId);
        Assert.Equal(ownerId, transaction.OwnerId);
        Assert.Equal(DateTimeKind.Utc, transaction.TransactionDate.Kind);
        Assert.True((DateTime.UtcNow - transaction.CreatedAt).TotalSeconds < 5);
        Assert.Null(transaction.UpdatedAt);
        Assert.False(transaction.IsDeleted);
    }

    [Theory]
    [InlineData(TransactionType.Expense, FlowType.Income)]
    [InlineData(TransactionType.Income, FlowType.Expense)]
    public void Constructor_WithIncompatibleFlowType_ShouldThrowDomainException(
        TransactionType type,
        FlowType categoryFlowType)
    {
        // Arrange
        var id = TransactionId.Unique();
        var description = new TransactionDescription("Mercado");
        var money = new Money(50.00m, "BRL");
        var accountId = AccountId.Unique();
        var categoryId = CategoryId.Unique();
        var ownerId = new OwnerId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Transaction(
            id,
            description,
            type,
            money,
            accountId,
            categoryId,
            ownerId,
            DateTime.UtcNow,
            categoryFlowType));

        Assert.Equal("O tipo da transação é incompatível com o tipo de fluxo permitido pela categoria.", exception.Message);
    }

    [Theory]
    [InlineData(TransactionType.Expense, FlowType.Expense)]
    [InlineData(TransactionType.Expense, FlowType.Both)]
    [InlineData(TransactionType.Income, FlowType.Income)]
    [InlineData(TransactionType.Income, FlowType.Both)]
    public void Constructor_WithCompatibleFlowType_ShouldSucceed(
        TransactionType type,
        FlowType categoryFlowType)
    {
        // Arrange
        var id = TransactionId.Unique();
        var description = new TransactionDescription("Mercado");
        var money = new Money(50.00m, "BRL");
        var accountId = AccountId.Unique();
        var categoryId = CategoryId.Unique();
        var ownerId = new OwnerId(Guid.NewGuid());

        // Act
        var transaction = new Transaction(
            id,
            description,
            type,
            money,
            accountId,
            categoryId,
            ownerId,
            DateTime.UtcNow,
            categoryFlowType);

        // Assert
        Assert.NotNull(transaction);
    }

    [Fact]
    public void Update_WithValidArguments_ShouldModifyPropertiesAndSetUpdatedAt()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionId.Unique(),
            new TransactionDescription("Aluguel"),
            TransactionType.Expense,
            new Money(1500.00m, "BRL"),
            AccountId.Unique(),
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            DateTime.UtcNow,
            FlowType.Expense);

        var newDesc = new TransactionDescription("Aluguel de Julho");
        var newMoney = new Money(1550.00m, "BRL");
        var newAccountId = AccountId.Unique();
        var newCategoryId = CategoryId.Unique();
        var newDate = DateTime.UtcNow.AddDays(1);

        // Act
        transaction.Update(newDesc, newMoney, newAccountId, newCategoryId, newDate, FlowType.Both);

        // Assert
        Assert.Equal(newDesc, transaction.Description);
        Assert.Equal(newMoney, transaction.Money);
        Assert.Equal(newAccountId, transaction.AccountId);
        Assert.Equal(newCategoryId, transaction.CategoryId);
        Assert.Equal(DateTimeKind.Utc, transaction.TransactionDate.Kind);
        Assert.NotNull(transaction.UpdatedAt);
        Assert.True((DateTime.UtcNow - transaction.UpdatedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public void Update_WithIncompatibleFlowType_ShouldThrowDomainException()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionId.Unique(),
            new TransactionDescription("Aluguel"),
            TransactionType.Expense,
            new Money(1500.00m, "BRL"),
            AccountId.Unique(),
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            DateTime.UtcNow,
            FlowType.Expense);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            transaction.Update(
                transaction.Description,
                transaction.Money,
                transaction.AccountId,
                transaction.CategoryId,
                transaction.TransactionDate,
                FlowType.Income));

        Assert.Equal("O tipo da transação é incompatível com o tipo de fluxo permitido pela categoria.", exception.Message);
    }

    [Fact]
    public void Archive_ShouldSetIsDeletedToTrueAndModifyUpdatedAt()
    {
        // Arrange
        var transaction = new Transaction(
            TransactionId.Unique(),
            new TransactionDescription("Aluguel"),
            TransactionType.Expense,
            new Money(1500.00m, "BRL"),
            AccountId.Unique(),
            CategoryId.Unique(),
            new OwnerId(Guid.NewGuid()),
            DateTime.UtcNow,
            FlowType.Expense);

        // Act
        transaction.Archive();

        // Assert
        Assert.True(transaction.IsDeleted);
        Assert.NotNull(transaction.UpdatedAt);
        Assert.True((DateTime.UtcNow - transaction.UpdatedAt.Value).TotalSeconds < 5);
    }
}
