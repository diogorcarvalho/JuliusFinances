namespace JuliusFinances.Core.Common.Domain;

/// <summary>
/// Contrato para o publicador de eventos de domínio.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publica um evento de domínio para seus respectivos assinantes de forma assíncrona.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
}
