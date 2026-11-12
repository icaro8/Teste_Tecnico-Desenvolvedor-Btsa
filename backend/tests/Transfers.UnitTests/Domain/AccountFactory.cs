using Transfers.Domain.Accounts;

namespace Transfers.UnitTests.Domain;

internal static class AccountFactory
{
    public static readonly DateTimeOffset Now = new(2026, 11, 12, 15, 0, 0, TimeSpan.Zero);

    private static readonly string[] ValidDocuments =
    [
        "52601815906", "08301661305", "18609139034", "99603082430", "62819482112", "99351819019"
    ];

    private static int _sequence;

    public static TransferLimitPolicy Limits(decimal dayAmount = 5_000m, int dayAttempts = 5, decimal nightAmount = 1_000m, int nightAttempts = 3) =>
        new(HourlyLimit.Create(dayAmount, dayAttempts).Value, HourlyLimit.Create(nightAmount, nightAttempts).Value);

    public static Account Open(decimal balance = 0m, decimal overdraftLimit = 0m, TransferLimitPolicy? limits = null)
    {
        var document = ValidDocuments[Interlocked.Increment(ref _sequence) % ValidDocuments.Length];
        return Account.Open("Titular Teste", document, balance, overdraftLimit, limits ?? Limits(), Now).Value;
    }
}
