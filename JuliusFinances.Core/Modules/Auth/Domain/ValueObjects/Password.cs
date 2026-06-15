using System.Text.RegularExpressions;
using JuliusFinances.Core.Common.Domain;
using JuliusFinances.Core.Common.Security;

namespace JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula a senha criptografada.
/// </summary>
public record Password
{
    private static readonly Regex StrengthRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        RegexOptions.Compiled);

    public string HashValue { get; }

    /// <summary>
    /// Construtor de materialização de dados (usado pelo EF Core ao ler do banco).
    /// </summary>
    public Password(string hashValue)
    {
        if (string.IsNullOrWhiteSpace(hashValue))
        {
            throw new DomainException("O hash da senha não pode ser vazio.");
        }
        HashValue = hashValue;
    }

    /// <summary>
    /// Fábrica para criar um objeto Password validando e criptografando uma senha em texto limpo.
    /// </summary>
    public static Password Create(string plainTextPassword, IPasswordHasher hasher)
    {
        ValidateStrength(plainTextPassword);
        var hash = hasher.Hash(plainTextPassword);
        return new Password(hash);
    }

    /// <summary>
    /// Valida se a senha em texto limpo atende aos critérios mínimos de segurança.
    /// </summary>
    public static void ValidateStrength(string plainTextPassword)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
        {
            throw new DomainException("A senha não pode ser nula ou vazia.");
        }

        if (!StrengthRegex.IsMatch(plainTextPassword))
        {
            throw new DomainException("A senha deve conter no mínimo 8 caracteres, pelo menos uma letra maiúscula, uma minúscula, um número e um caractere especial.");
        }
    }

    /// <summary>
    /// Compara a senha em texto limpo com o hash atual.
    /// </summary>
    public bool Verify(string plainTextPassword, IPasswordHasher hasher)
    {
        return hasher.Verify(plainTextPassword, HashValue);
    }
}
