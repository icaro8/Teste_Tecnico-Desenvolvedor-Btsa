using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Transfers.Infrastructure.Persistence.Seeding;

namespace Transfers.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public const string RunSeedsKey = "RUN_SEEDS";

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        if (provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ApplyMigrationsOnStartup)
        {
            await provider.GetRequiredService<TransfersDbContext>().Database.MigrateAsync(cancellationToken);
        }

        if (provider.GetRequiredService<IConfiguration>().GetValue<bool>(RunSeedsKey))
        {
            await provider.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
        }
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TransfersDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public static async Task SeedDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TransfersDbContext>().Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
    }
}
