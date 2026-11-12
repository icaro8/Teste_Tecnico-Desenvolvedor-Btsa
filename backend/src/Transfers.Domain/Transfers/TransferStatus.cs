namespace Transfers.Domain.Transfers;

public enum TransferStatus
{
    Scheduled,
    Processing,
    Completed,
    Failed,
    Cancelled
}
