using System.Collections.Concurrent;
using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Common.Security;
using JuliusFinances.Core.Modules.Auth.Application.Interfaces;
using JuliusFinances.Core.Modules.Auth.Application.UseCases.RegisterUser;
using JuliusFinances.Core.Modules.Auth.Domain.Entities;
using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;
using JuliusFinances.Core.Modules.Auth.Domain.Exceptions;

namespace JuliusFinances.Tests.UseCases;

public class RegisterUserUseCaseTests
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

    private class FakeDomainEventPublisher : IDomainEventPublisher
    {
        public readonly List<IDomainEvent> PublishedEvents = new();

        public Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
        {
            PublishedEvents.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithValidRequest_ShouldCreateUserAndReturnResponse()
    {
        // Arrange
        var repository = new InMemoryUserRepository();
        var hasher = new FakePasswordHasher();
        var publisher = new FakeDomainEventPublisher();
        var useCase = new RegisterUserUseCase(repository, hasher, publisher);
        var request = new RegisterUserRequest("Diogo Silva", "diogo@exemplo.com", "SenhaSegura123!");

        // Act
        var response = await useCase.ExecuteAsync(request);

        // Assert
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("Diogo Silva", response.Name);
        Assert.Equal("diogo@exemplo.com", response.Email);

        // Verify state inside in-memory repository
        Assert.Single(repository.Users);
        var savedUser = repository.Users[response.Id];
        Assert.Equal("Diogo Silva", savedUser.Name.Value);
        Assert.Equal("diogo@exemplo.com", savedUser.Email.Value);
        Assert.Equal("hashed_SenhaSegura123!", savedUser.Password.HashValue);

        // Verify event published
        Assert.Single(publisher.PublishedEvents);
        var publishedEvent = Assert.IsType<JuliusFinances.Core.Modules.Auth.Domain.Events.UserRegisteredEvent>(publisher.PublishedEvents[0]);
        Assert.Equal(response.Id, publishedEvent.UserId);
    }

    [Fact]
    public async Task ExecuteAsync_WithDuplicateEmail_ShouldThrowEmailAlreadyExistsException()
    {
        // Arrange
        var repository = new InMemoryUserRepository();
        var hasher = new FakePasswordHasher();
        var publisher = new FakeDomainEventPublisher();

        // Cadastra um usuário pré-existente
        var existingUser = new User(
            UserId.Unique(),
            new Name("Pre Existente"),
            new Email("duplicado@exemplo.com"),
            new Password("some_hash"));
        await repository.AddAsync(existingUser);

        var useCase = new RegisterUserUseCase(repository, hasher, publisher);
        var request = new RegisterUserRequest("Novo Nome", "duplicado@exemplo.com", "SenhaSegura123!");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => useCase.ExecuteAsync(request));
        Assert.Equal("O e-mail 'duplicado@exemplo.com' já está cadastrado no sistema.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithWeakPassword_ShouldThrowDomainExceptionBeforeCreatingObjects()
    {
        // Arrange
        var repository = new InMemoryUserRepository();
        var hasher = new FakePasswordHasher();
        var publisher = new FakeDomainEventPublisher();
        var useCase = new RegisterUserUseCase(repository, hasher, publisher);
        var request = new RegisterUserRequest("Diogo Silva", "diogo@exemplo.com", "senha");

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(request));
        Assert.Empty(repository.Users); // Nenhum usuário deve ser criado
    }
}
