using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transfers.Infrastructure.Persistence.Outbox;

namespace Transfers.Infrastructure.BackgroundJobs;

internal sealed partial class OutboxWorker(
    IServiceScopeFactory scopes,
    IOptions<OutboxOptions> options,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(options.Value.PollingIntervalMilliseconds));

        do
        {
            await DrainAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task DrainAsync(CancellationToken stoppingToken)
    {
        try
        {
            int processed;
            do
            {
                await using var scope = scopes.CreateAsyncScope();
                processed = await scope.ServiceProvider.GetRequiredService<OutboxProcessor>().ProcessBatchAsync(stoppingToken);
            }
            while (processed == options.Value.BatchSize);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogProcessingFailure(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox processing failed; retrying on the next tick")]
    private partial void LogProcessingFailure(Exception exception);
}
