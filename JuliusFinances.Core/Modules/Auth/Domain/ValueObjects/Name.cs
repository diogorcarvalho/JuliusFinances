using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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

        var normalized = Regex.Replace(value.Trim(), @"\s+", " ");
        if (normalized.Length < 3 || normalized.Length > 150)
        {
            throw new DomainException("O nome deve conter entre 3 e 150 caracteres.");
        }

        Value = Capitalize(normalized);
    }

    private static string Capitalize(string text)
    {
        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        var titleCased = textInfo.ToTitleCase(text.ToLowerInvariant());

        var prepositions = new[] { "de", "di", "do", "da", "dos", "das", "e" };
        var words = titleCased.Split(' ');

        for (int i = 1; i < words.Length; i++)
        {
            if (prepositions.Contains(words[i].ToLowerInvariant()))
            {
                words[i] = words[i].ToLowerInvariant();
            }
        }

        return string.Join(" ", words);
    }
}
