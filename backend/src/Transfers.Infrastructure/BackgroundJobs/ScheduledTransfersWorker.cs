using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transfers.Application.Abstractions;
using Transfers.Application.Transfers;

namespace Transfers.Infrastructure.BackgroundJobs;

internal sealed partial class ScheduledTransfersWorker(
    IServiceScopeFactory scopes,
    IOptions<ScheduledTransfersOptions> options,
    ILogger<ScheduledTransfersWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds));

        do
        {
            await ProcessDueTransfersAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessDueTransfersAsync(CancellationToken stoppingToken)
    {
        for (var processed = 0; processed < options.Value.BatchSize; processed++)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ProcessDueTransferCommand, bool>>();
                var result = await handler.HandleAsync(new ProcessDueTransferCommand(), stoppingToken);

                if (result.IsFailure || !result.Value)
                {
                    return;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogProcessingFailure(exception);
                return;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduled transfer processing failed; retrying on the next tick")]
    private partial void LogProcessingFailure(Exception exception);
}
