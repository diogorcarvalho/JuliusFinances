using Microsoft.Extensions.DependencyInjection;
using JuliusFinances.Core.Common.Domain;

namespace JuliusFinances.Api.Common.Events;

/// <summary>
/// Implementação concreta do publicador de eventos de domínio em memória utilizando o container de DI nativo.
/// </summary>
public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
    {
        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
