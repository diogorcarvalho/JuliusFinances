using JuliusFinances.Core.Common.Domain;
using System.Globalization;
using System.Text;

namespace JuliusFinances.Core.Modules.FinancesSetup.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula, valida e normaliza o nome da categoria.
/// </summary>
public record CategoryName
{
    public string Value { get; }

    public CategoryName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("O nome da categoria não pode ser nulo ou vazio.");
        }

        // Remove espaços sobressalentes e múltiplos espaços internos
        var trimmed = string.Join(" ", value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        if (trimmed.Length < 3 || trimmed.Length > 100)
        {
            throw new DomainException("O nome da categoria deve conter entre 3 e 100 caracteres.");
        }

        // Padroniza a capitalização: primeira letra maiúscula, mantendo o restante
        Value = char.ToUpper(trimmed[0]) + trimmed.Substring(1);
    }

    /// <summary>
    /// Retorna o nome da categoria normalizado para comparação (sem acentos e em minúsculas).
    /// </summary>
    public string GetNormalizedForComparison()
    {
        var text = Value.ToLowerInvariant();

        // Substituições explícitas para garantir funcionamento mesmo em ambientes com Invariant Globalization (como alguns containers Linux)
        var sb = new StringBuilder(text);
        sb.Replace("á", "a").Replace("à", "a").Replace("â", "a").Replace("ã", "a").Replace("ä", "a")
          .Replace("é", "e").Replace("è", "e").Replace("ê", "e").Replace("ë", "e")
          .Replace("í", "i").Replace("ì", "i").Replace("î", "i").Replace("ï", "i")
          .Replace("ó", "o").Replace("ò", "o").Replace("ô", "o").Replace("õ", "o").Replace("ö", "o")
          .Replace("ú", "u").Replace("ù", "u").Replace("û", "u").Replace("ü", "u")
          .Replace("ç", "c").Replace("ñ", "n");

        var textWithoutAccents = sb.ToString();
        var normalizedString = textWithoutAccents.Normalize(NormalizationForm.FormD);
        var resultBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                resultBuilder.Append(c);
            }
        }

        return resultBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
