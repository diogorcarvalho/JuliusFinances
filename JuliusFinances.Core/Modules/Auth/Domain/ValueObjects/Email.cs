using System.Text.RegularExpressions;
using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que valida o formato de e-mail e garante consistência.
/// </summary>
public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("O e-mail não pode ser nulo ou vazio.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
        {
            throw new DomainException("O e-mail informado está em um formato inválido.");
        }

        Value = normalized;
    }
}
