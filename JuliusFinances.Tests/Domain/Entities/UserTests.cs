using JuliusFinances.Core.Modules.Auth.Domain.Entities;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateUserWithCreatedAt()
    {
        // Arrange
        var id = UserId.Unique();
        var name = new Name("Diogo Silva");
        var email = new Email("diogo@exemplo.com");
        var password = new Password("some_hashed_password");

        // Act
        var user = new User(id, name, email, password);

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal(name, user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(password, user.Password);
        Assert.True((DateTime.UtcNow - user.CreatedAt).TotalSeconds < 5);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void UpdateProfile_WithValidArguments_ShouldModifyPropertiesAndUpdateTime()
    {
        // Arrange
        var user = new User(
            UserId.Unique(),
            new Name("Diogo Silva"),
            new Email("diogo@exemplo.com"),
            new Password("some_hashed_password"));

        var newName = new Name("Diogo S.");
        var newEmail = new Email("diogo.s@exemplo.com");

        // Act
        user.UpdateProfile(newName, newEmail);

        // Assert
        Assert.Equal(newName, user.Name);
        Assert.Equal(newEmail, user.Email);
        Assert.NotNull(user.UpdatedAt);
        Assert.True((DateTime.UtcNow - user.UpdatedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public void UpdatePassword_WithValidPassword_ShouldModifyPasswordPropertyAndUpdateTime()
    {
        // Arrange
        var user = new User(
            UserId.Unique(),
            new Name("Diogo Silva"),
            new Email("diogo@exemplo.com"),
            new Password("some_hashed_password"));

        var newPassword = new Password("new_hashed_password");

        // Act
        user.UpdatePassword(newPassword);

        // Assert
        Assert.Equal(newPassword, user.Password);
        Assert.NotNull(user.UpdatedAt);
        Assert.True((DateTime.UtcNow - user.UpdatedAt.Value).TotalSeconds < 5);
    }
}
