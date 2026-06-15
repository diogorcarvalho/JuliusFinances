using JuliusFinances.Core.Modules.Auth.Domain.Entities;

namespace JuliusFinances.Core.Modules.Auth.Application.Interfaces;

/// <summary>
/// Representa o token gerado e seu tempo de expiração.
/// </summary>
public record GeneratedToken(string Token, int ExpiresInMinutes);

/// <summary>
/// Contrato para geração de tokens JWT seguros.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Gera um token de acesso seguro para o usuário informado.
    /// </summary>
    GeneratedToken GenerateToken(User user);
}
