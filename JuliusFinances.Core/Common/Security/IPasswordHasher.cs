namespace JuliusFinances.Core.Common.Security;

/// <summary>
/// Contrato para geração e verificação de hashes de senha.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Gera um hash seguro a partir de uma senha válida.
    /// </summary>
    string Hash(string plainTextPassword);

    /// <summary>
    /// Compara a senha em texto limpo com o hash armazenado.
    /// </summary>
    bool Verify(string plainTextPassword, string hashedPassword);
}
