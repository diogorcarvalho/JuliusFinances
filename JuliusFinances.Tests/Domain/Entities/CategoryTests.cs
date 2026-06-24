using JuliusFinances.Core.Modules.FinancesSetup.Domain.Entities;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.Enums;

namespace JuliusFinances.Tests.Domain.Entities;

public class CategoryTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateCategoryWithCreatedAtAndNotDeleted()
    {
        // Arrange
        var id = CategoryId.Unique();
        var name = new CategoryName("Alimentação");
        var flowType = FlowType.Expense;
        var ownerId = new OwnerId(Guid.NewGuid());

        // Act
        var category = new Category(id, name, flowType, ownerId);

        // Assert
        Assert.Equal(id, category.Id);
        Assert.Equal(name, category.Name);
        Assert.Equal(flowType, category.FlowType);
        Assert.Equal(ownerId, category.OwnerId);
        Assert.True((DateTime.UtcNow - category.CreatedAt).TotalSeconds < 5);
        Assert.Null(category.UpdatedAt);
        Assert.False(category.IsDeleted);
    }

    [Fact]
    public void Constructor_WithNullOwnerId_ShouldBeGlobalCategory()
    {
        // Arrange
        var id = CategoryId.Unique();
        var name = new CategoryName("Alimentação");
        var flowType = FlowType.Expense;

        // Act
        var category = new Category(id, name, flowType, null);

        // Assert
        Assert.Null(category.OwnerId);
        Assert.False(category.IsDeleted);
    }

    [Fact]
    public void Update_WithValidArguments_ShouldModifyPropertiesAndUpdateTime()
    {
        // Arrange
        var category = new Category(
            CategoryId.Unique(),
            new CategoryName("Alimentação"),
            FlowType.Expense,
            null);

        var newName = new CategoryName("Supermercado & Alimentação");
        var newFlowType = FlowType.Both;

        // Act
        category.Update(newName, newFlowType);

        // Assert
        Assert.Equal(newName, category.Name);
        Assert.Equal(newFlowType, category.FlowType);
        Assert.NotNull(category.UpdatedAt);
        Assert.True((DateTime.UtcNow - category.UpdatedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public void Archive_ShouldSetIsDeletedToTrueAndModifyUpdateTime()
    {
        // Arrange
        var category = new Category(
            CategoryId.Unique(),
            new CategoryName("Alimentação"),
            FlowType.Expense,
            null);

        // Act
        category.Archive();

        // Assert
        Assert.True(category.IsDeleted);
        Assert.NotNull(category.UpdatedAt);
        Assert.True((DateTime.UtcNow - category.UpdatedAt.Value).TotalSeconds < 5);
    }
}
