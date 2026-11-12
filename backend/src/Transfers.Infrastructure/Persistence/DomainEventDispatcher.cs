using Microsoft.Extensions.DependencyInjection;
using Transfers.Application.Abstractions;
using Transfers.Domain.Common;

namespace Transfers.Infrastructure.Persistence;

internal sealed class DomainEventDispatcher(IServiceProvider services)
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handle = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

            foreach (var handler in services.GetServices(handlerType))
            {
                await (Task)handle.Invoke(handler, [domainEvent, cancellationToken])!;
            }
        }
    }
}
