using System.Collections.Concurrent;
using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Common.Security;
using JuliusFinances.Core.Modules.Auth.Application.Interfaces;
using JuliusFinances.Core.Modules.Auth.Application.UseCases.Login;
using JuliusFinances.Core.Modules.Auth.Domain.Entities;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Tests.UseCases;

public class LoginUseCaseTests
{
    private class InMemoryUserRepository : IUserRepository
    {
        public readonly ConcurrentDictionary<Guid, User> Users = new();

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Users[user.Id.Value] = user;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            var exists = Users.Values.Any(u => u.Email.Value == email.Value);
            return Task.FromResult(exists);
        }

        public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
        {
            var user = Users.Values.FirstOrDefault(u => u.Email.Value == email.Value);
            return Task.FromResult(user);
        }

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            Users.TryGetValue(id.Value, out var user);
            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            Users[user.Id.Value] = user;
            return Task.CompletedTask;
        }
    }

    private class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string plainTextPassword) => $"hashed_{plainTextPassword}";
        public bool Verify(string plainTextPassword, string hashedPassword) => hashedPassword == $"hashed_{plainTextPassword}";
    }

    private class FakeJwtTokenGenerator : IJwtTokenGenerator
    {
        public GeneratedToken GenerateToken(User user)
        {
            return new GeneratedToken($"token_for_{user.Email.Value}", 60);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ShouldReturnTokenAndUserDto()
    {
        // Arrange
        var repository = new InMemoryUserRepository();
        var hasher = new FakePasswordHasher();
        var tokenGenerator = new FakeJwtTokenGenerator();

        // Cadastra o usuário válido previamente
        var userId = UserId.Unique();
        var user = new User(
            userId,
            new Name("Diogo Silva"),
            new Email("diogo@exemplo.com"),
            new Password("hashed_SenhaSegura123!"));
        await repository.AddAsync(user);

        var useCase = new LoginUseCase(repository, hasher, tokenGenerator);
        var request = new LoginRequest("diogo@exemplo.com", "SenhaSegura123!");

        // Act
        var response = await useCase.ExecuteAsync(request);

        // Assert
        Assert.Equal("token_for_diogo@exemplo.com", response.AccessToken);
        Assert.Equal(60, response.ExpiresInMinutes);
        Assert.Equal(userId.Value, response.User.Id);
        Assert.Equal("Diogo Silva", response.User.Name);
        Assert.Equal("diogo@exemplo.com", response.User.Email);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentUser_ShouldThrowDomainExceptionWithGenericMessage()
    {
        // Arrange
        var repository = new InMemoryUserRepository();
        var hasher = new FakePasswordHasher();
        var tokenGenerator = new FakeJwtTokenGenerator();

        var useCase = new LoginUseCase(repository, hasher, tokenGenerator);
        var request = new LoginRequest("inexistente@exemplo.com", "SenhaSegura123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(request));
        Assert.Equal("E-mail ou senha incorretos.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithWrongPassword_ShouldThrowDomainExceptionWithGenericMessage()
    {
        // Arrange
        var repository = new InMemoryUserRepository();
        var hasher = new FakePasswordHasher();
        var tokenGenerator = new FakeJwtTokenGenerator();

        // Cadastra o usuário válido previamente
        var user = new User(
            UserId.Unique(),
            new Name("Diogo Silva"),
            new Email("diogo@exemplo.com"),
            new Password("hashed_SenhaSegura123!"));
        await repository.AddAsync(user);

        var useCase = new LoginUseCase(repository, hasher, tokenGenerator);
        var request = new LoginRequest("diogo@exemplo.com", "SenhaIncorreta123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(request));
        Assert.Equal("E-mail ou senha incorretos.", exception.Message);
    }
}
