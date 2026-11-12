using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Seeding;

internal sealed class DemoDataSeeder(TransfersDbContext db, IOptions<TransferRulesOptions> rules, TimeProvider clock)
{
    private sealed record DemoAccount(string Name, string Document, decimal Balance, decimal OverdraftLimit, AccountStatus Status);

    private static readonly DemoAccount[] DemoAccounts =
    [
        new("Ana Souza", "52601815906", 500m, 1_000m, AccountStatus.Active),
        new("Bruno Lima", "08301661305", 2_000m, 1_000m, AccountStatus.Active),
        new("Carla Mendes", "18609139034", 15_000m, 0m, AccountStatus.Active),
        new("Diego Rocha", "99603082430", 1_200m, 500m, AccountStatus.Active),
        new("Elisa Martins", "62819482112", 300m, 2_000m, AccountStatus.Active),
        new("Fábio Teixeira", "99351819019", 800m, 300m, AccountStatus.Blocked),
        new("Gabriela Nunes", "93786579741", 50m, 0m, AccountStatus.Inactive)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await db.Accounts.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = clock.GetUtcNow();
        var history = now.AddDays(-2);
        var limits = DefaultLimits();
        var accounts = DemoAccounts.Select(demo => Open(demo, limits, history.AddDays(-30))).ToList();

        var ana = accounts[0];
        var bruno = accounts[1];
        var carla = accounts[2];
        var diego = accounts[3];
        var elisa = accounts[4];

        db.Accounts.AddRange(accounts);
        db.Transfers.AddRange(
            Settled(carla, elisa, 1_500m, history),
            Settled(bruno, diego, 3_200m, history.AddMinutes(10)),
            Settled(diego, ana, 250m, history.AddHours(3)),
            Settled(ana, bruno, 900m, history.AddHours(1)),
            Scheduled(carla, ana, 700m, now.AddDays(1), history),
            Scheduled(bruno, elisa, 450m, now.AddDays(3), history));

        await db.SaveChangesAsync(cancellationToken);
    }

    private TransferLimitPolicy DefaultLimits()
    {
        var daytime = rules.Value.DefaultDaytimeLimit;
        var nighttime = rules.Value.DefaultNighttimeLimit;
        return new TransferLimitPolicy(
            HourlyLimit.Create(daytime.MaxAmount, daytime.MaxAttempts).Value,
            HourlyLimit.Create(nighttime.MaxAmount, nighttime.MaxAttempts).Value);
    }

    private static Account Open(DemoAccount demo, TransferLimitPolicy limits, DateTimeOffset openedAt)
    {
        var account = Account.Open(demo.Name, demo.Document, demo.Balance, demo.OverdraftLimit, limits, openedAt).Value;
        var statusChange = demo.Status switch
        {
            AccountStatus.Blocked => account.Block(),
            AccountStatus.Inactive => account.Deactivate(),
            _ => Result.Success()
        };

        return statusChange.IsSuccess ? account : throw new InvalidOperationException(statusChange.Error!.Message);
    }

    private static Transfer Settled(Account source, Account destination, decimal amount, DateTimeOffset at)
    {
        var transfer = Transfer.CreateImmediate(source.Id, destination.Id, amount, null, at).Value;
        FundsTransferService.Settle(transfer, source, destination, HourlyTransferUsage.None, TransferPeriod.Daytime, at);
        transfer.ClearDomainEvents();
        return transfer;
    }

    private static Transfer Scheduled(Account source, Account destination, decimal amount, DateTimeOffset scheduledFor, DateTimeOffset at)
    {
        var transfer = Transfer.Schedule(source.Id, destination.Id, amount, scheduledFor, null, at).Value;
        transfer.ClearDomainEvents();
        return transfer;
    }
}
