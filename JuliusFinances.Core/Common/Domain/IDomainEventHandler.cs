namespace JuliusFinances.Core.Common.Domain;

/// <summary>
/// Contrato para manipuladores de eventos de domínio.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Manipula o evento de domínio de forma assíncrona.
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
