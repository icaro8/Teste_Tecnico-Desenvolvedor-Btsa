namespace Transfers.Domain.Transfers;

public sealed record HourlyTransferUsage(int Attempts, decimal TransferredAmount)
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);

    public static readonly HourlyTransferUsage None = new(0, 0);
}
