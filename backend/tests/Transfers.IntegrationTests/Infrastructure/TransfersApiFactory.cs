using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Transfers.Application.Abstractions;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;
using Transfers.Infrastructure.Persistence.Outbox;

namespace Transfers.IntegrationTests.Infrastructure;

public sealed class TransfersApiFactory(PostgresFixture postgres) : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Afternoon = new(2026, 11, 12, 15, 0, 0, TimeSpan.FromHours(-3));

    public FakeTimeProvider Clock { get; } = new(Afternoon.ToUniversalTime());

    public RecordingEventHandler Events { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Transfers", postgres.ConnectionStringFor($"transfers_{Guid.NewGuid():N}"));
        builder.UseSetting("ScheduledTransfers:Enabled", "false");
        builder.UseSetting("Outbox:Enabled", "false");
        builder.UseSetting("RUN_SEEDS", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IDomainEventHandler<TransferCompleted>>(Events);
            services.AddSingleton<IDomainEventHandler<TransferCancelled>>(Events);
        });
    }

    public async Task<int> ProcessOutboxAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcessBatchAsync(CancellationToken.None);
    }

    public async Task<int> ProcessDueTransfersAsync()
    {
        var processed = 0;
        while (true)
        {
            await using var scope = Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ProcessDueTransferCommand, bool>>();
            var result = await handler.HandleAsync(new ProcessDueTransferCommand(), CancellationToken.None);
            if (!result.Value)
            {
                return processed;
            }

            processed++;
        }
    }
}
