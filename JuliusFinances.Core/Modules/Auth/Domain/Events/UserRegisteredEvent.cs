using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Core.Modules.Auth.Domain.Events;

/// <summary>
/// Evento de domínio disparado quando um novo usuário se cadastra com sucesso no sistema.
/// </summary>
public record UserRegisteredEvent(Guid UserId) : IDomainEvent;
