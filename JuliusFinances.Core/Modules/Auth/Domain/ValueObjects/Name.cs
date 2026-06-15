using System.Globalization;
using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula e valida o nome do usuário.
/// </summary>
public record Name
{
    public string Value { get; }

    public Name(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("O nome não pode ser nulo ou vazio.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 150)
        {
            throw new DomainException("O nome deve conter entre 3 e 150 caracteres.");
        }

        Value = Capitalize(trimmed);
    }

    private static string Capitalize(string text)
    {
        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        return textInfo.ToTitleCase(text.ToLowerInvariant());
    }
}
