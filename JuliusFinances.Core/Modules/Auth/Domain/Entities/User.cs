using JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

namespace JuliusFinances.Core.Modules.Auth.Domain.Entities;

/// <summary>
/// Entidade de domínio rica que representa um usuário no sistema.
/// </summary>
public class User
{
    public UserId Id { get; private set; } = null!;
    public Name Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Password Password { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Construtor privado para materialização do EF Core
    private User() { }

    /// <summary>
    /// Cria uma nova instância de usuário com estado válido inicial.
    /// </summary>
    public User(UserId id, Name name, Email email, Password password)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza as informações de perfil do usuário.
    /// </summary>
    public void UpdateProfile(Name name, Email email)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza de forma segura a senha do usuário.
    /// </summary>
    public void UpdatePassword(Password password)
    {
        Password = password ?? throw new ArgumentNullException(nameof(password));
        UpdatedAt = DateTime.UtcNow;
    }
}
