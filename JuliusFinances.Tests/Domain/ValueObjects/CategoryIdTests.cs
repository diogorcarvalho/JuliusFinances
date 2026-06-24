using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.ValueObjects;

public class CategoryIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateInstance()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var categoryId = new CategoryId(guid);

        // Assert
        Assert.Equal(guid, categoryId.Value);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrowDomainException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => new CategoryId(Guid.Empty));
        Assert.Equal("O identificador da categoria não pode ser vazio.", exception.Message);
    }

    [Fact]
    public void Unique_ShouldGenerateValidAndUniqueCategoryIds()
    {
        // Act
        var id1 = CategoryId.Unique();
        var id2 = CategoryId.Unique();

        // Assert
        Assert.NotEqual(Guid.Empty, id1.Value);
        Assert.NotEqual(Guid.Empty, id2.Value);
        Assert.NotEqual(id1, id2);
    }
}
