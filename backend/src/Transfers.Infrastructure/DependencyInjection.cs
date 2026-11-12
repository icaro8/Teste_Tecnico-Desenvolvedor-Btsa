using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Infrastructure.BackgroundJobs;
using Transfers.Infrastructure.Persistence;
using Transfers.Infrastructure.Persistence.Outbox;
using Transfers.Infrastructure.Persistence.Repositories;
using Transfers.Infrastructure.Persistence.Seeding;

namespace Transfers.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Transfers";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");
        }

        services.Configure<TransferRulesOptions>(configuration.GetSection(TransferRulesOptions.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<ScheduledTransfersOptions>(configuration.GetSection(ScheduledTransfersOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddDbContext<TransfersDbContext>(options => ConfigureDbContext(options, connectionString));
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ITransferRepository, TransferRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<DomainEventDispatcher>();
        services.AddScoped<OutboxProcessor>();
        services.AddScoped<DemoDataSeeder>();

        if (configuration.GetSection(ScheduledTransfersOptions.SectionName).Get<ScheduledTransfersOptions>()?.Enabled ?? true)
        {
            services.AddHostedService<ScheduledTransfersWorker>();
        }

        if (configuration.GetSection(OutboxOptions.SectionName).Get<OutboxOptions>()?.Enabled ?? true)
        {
            services.AddHostedService<OutboxWorker>();
        }

        return services;
    }

    internal static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString) =>
        options
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3))
            .UseSnakeCaseNamingConvention();
}
