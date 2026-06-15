using System.ComponentModel.DataAnnotations;

namespace JuliusFinances.Api.Common.Security;

/// <summary>
/// Opções de configuração para o Token JWT com suporte a DataAnnotations e validação ativa no startup.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required(ErrorMessage = "O segredo de assinatura do JWT (Secret) é obrigatório.")]
    [MinLength(32, ErrorMessage = "O segredo de assinatura do JWT (Secret) deve conter pelo menos 32 caracteres.")]
    public string Secret { get; set; } = string.Empty;

    [Range(1, 10080, ErrorMessage = "A expiração do token (ExpiryInMinutes) deve ser entre 1 minuto e 1 semana.")]
    public int ExpiryInMinutes { get; set; } = 60;
}
