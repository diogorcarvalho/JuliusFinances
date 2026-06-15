namespace JuliusFinances.Core.Common.Domain;

/// <summary>
/// Exceção base para todas as violações de regras de negócio e validações do domínio.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
