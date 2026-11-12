using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Transfers.Infrastructure.Persistence;

internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TransfersDbContext>
{
    public TransfersDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Transfers") ?? "Host=localhost;Database=transfers";
        var options = new DbContextOptionsBuilder<TransfersDbContext>();
        DependencyInjection.ConfigureDbContext(options, connectionString);
        return new TransfersDbContext(options.Options);
    }
}
