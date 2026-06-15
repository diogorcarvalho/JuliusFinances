using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.Auth.Domain.ValueObjects;

/// <summary>
/// Objeto de Valor que encapsula o identificador único do usuário (Strongly Typed ID).
/// </summary>
public record UserId
{
    public Guid Value { get; }

    public UserId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("O identificador do usuário não pode ser vazio.");
        }
        Value = value;
    }

    public static UserId Unique() => new(Guid.NewGuid());
}
