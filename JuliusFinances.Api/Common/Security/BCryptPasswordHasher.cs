using JuliusFinances.Core.Common.Security;

namespace JuliusFinances.Api.Common.Security;

/// <summary>
/// Implementação concreta do IPasswordHasher utilizando o algoritmo robusto BCrypt.
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainTextPassword)
    {
        // EnhancedHashPassword do BCrypt.Net-Next oferece suporte extra a strings longas
        return BCrypt.Net.BCrypt.EnhancedHashPassword(plainTextPassword, 11);
    }

    public bool Verify(string plainTextPassword, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(plainTextPassword, hashedPassword);
    }
}
