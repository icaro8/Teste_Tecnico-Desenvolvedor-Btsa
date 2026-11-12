using System.Text.Json;
using System.Text.Json.Serialization;
using Transfers.Domain.Common;

namespace Transfers.Infrastructure.Persistence.Outbox;

internal static class OutboxSerializer
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Dictionary<string, Type> EventTypes = typeof(IDomainEvent).Assembly
        .GetTypes()
        .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(type))
        .ToDictionary(type => type.Name);

    public static string TypeNameOf(IDomainEvent domainEvent) => domainEvent.GetType().Name;

    public static string Serialize(IDomainEvent domainEvent) => JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Json);

    public static IDomainEvent Deserialize(OutboxMessage message)
    {
        if (!EventTypes.TryGetValue(message.Type, out var type))
        {
            throw new InvalidOperationException($"Unknown domain event type '{message.Type}'.");
        }

        return (IDomainEvent)(JsonSerializer.Deserialize(message.Payload, type, Json)
            ?? throw new InvalidOperationException($"Outbox message {message.Id} has an empty payload."));
    }
}
