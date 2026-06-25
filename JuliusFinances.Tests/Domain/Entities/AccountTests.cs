using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;

namespace JuliusFinances.Tests.Domain.Entities;

public class AccountTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateAccountWithCreatedAtAndNotDeleted()
    {
        // Arrange
        var id = AccountId.Unique();
        var name = new AccountName("Conta Corrente Itaú");
        var type = AccountType.CheckingAccount;
        var initialBalance = 1000.00m;
        var ownerId = new OwnerId(Guid.NewGuid());

        // Act
        var account = new Account(id, name, type, initialBalance, ownerId);

        // Assert
        Assert.Equal(id, account.Id);
        Assert.Equal(name, account.Name);
        Assert.Equal(type, account.Type);
        Assert.Equal(initialBalance, account.InitialBalance);
        Assert.Equal(ownerId, account.OwnerId);
        Assert.True((DateTime.UtcNow - account.CreatedAt).TotalSeconds < 5);
        Assert.Null(account.UpdatedAt);
        Assert.False(account.IsDeleted);
    }

    [Fact]
    public void Constructor_WithCashTypeAndNegativeBalance_ShouldThrowDomainException()
    {
        // Arrange
        var id = AccountId.Unique();
        var name = new AccountName("Carteira");
        var type = AccountType.Cash;
        var initialBalance = -10.00m;
        var ownerId = new OwnerId(Guid.NewGuid());

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new Account(id, name, type, initialBalance, ownerId));
        Assert.Equal("Contas do tipo 'Dinheiro em Espécie' não podem possuir saldo inicial negativo.", exception.Message);
    }

    [Fact]
    public void Update_WithValidArguments_ShouldModifyPropertiesAndUpdateTime()
    {
        // Arrange
        var account = new Account(
            AccountId.Unique(),
            new AccountName("Carteira"),
            AccountType.Cash,
            100.00m,
            new OwnerId(Guid.NewGuid()));

        var newName = new AccountName("Carteira Do Diogo");
        var newType = AccountType.CheckingAccount;
        var newBalance = 150.00m;

        // Act
        account.Update(newName, newType, newBalance, hasTransactions: false);

        // Assert
        Assert.Equal(newName, account.Name);
        Assert.Equal(newType, account.Type);
        Assert.Equal(newBalance, account.InitialBalance);
        Assert.NotNull(account.UpdatedAt);
        Assert.True((DateTime.UtcNow - account.UpdatedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public void Update_WithBalanceChangeAndHasTransactions_ShouldThrowDomainException()
    {
        // Arrange
        var account = new Account(
            AccountId.Unique(),
            new AccountName("Carteira"),
            AccountType.Cash,
            100.00m,
            new OwnerId(Guid.NewGuid()));

        var newName = new AccountName("Carteira Nova");

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            account.Update(newName, AccountType.Cash, 150.00m, hasTransactions: true));

        Assert.Equal("O saldo inicial não pode ser alterado após o registro de transações.", exception.Message);
    }

    [Fact]
    public void Update_WithNoBalanceChangeAndHasTransactions_ShouldSucceed()
    {
        // Arrange
        var account = new Account(
            AccountId.Unique(),
            new AccountName("Carteira"),
            AccountType.Cash,
            100.00m,
            new OwnerId(Guid.NewGuid()));

        var newName = new AccountName("Carteira Do Diogo");

        // Act
        account.Update(newName, AccountType.Cash, 100.00m, hasTransactions: true);

        // Assert
        Assert.Equal(newName, account.Name);
        Assert.Equal(100.00m, account.InitialBalance);
    }

    [Fact]
    public void Update_ToCashTypeWithNegativeBalance_ShouldThrowDomainException()
    {
        // Arrange
        var account = new Account(
            AccountId.Unique(),
            new AccountName("Itaú"),
            AccountType.CheckingAccount,
            -50.00m,
            new OwnerId(Guid.NewGuid()));

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            account.Update(account.Name, AccountType.Cash, null, hasTransactions: false));

        Assert.Equal("Contas do tipo 'Dinheiro em Espécie' não podem possuir saldo inicial negativo.", exception.Message);
    }

    [Fact]
    public void Archive_ShouldSetIsDeletedToTrueAndModifyUpdateTime()
    {
        // Arrange
        var account = new Account(
            AccountId.Unique(),
            new AccountName("Carteira"),
            AccountType.Cash,
            0.00m,
            new OwnerId(Guid.NewGuid()));

        // Act
        account.Archive();

        // Assert
        Assert.True(account.IsDeleted);
        Assert.NotNull(account.UpdatedAt);
        Assert.True((DateTime.UtcNow - account.UpdatedAt.Value).TotalSeconds < 5);
    }
}
