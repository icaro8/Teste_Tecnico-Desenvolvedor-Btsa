using Transfers.Domain.Common;

namespace Transfers.Domain.Transfers;

public static class TransferErrors
{
    public static readonly Error InvalidAmount =
        Error.Validation("transfer.invalid_amount", "O valor da transferência deve ser maior que zero e ter no máximo 2 casas decimais.");

    public static readonly Error SameAccount =
        Error.Validation("transfer.same_account", "A conta de origem e a de destino devem ser diferentes.");

    public static readonly Error ScheduleMustBeInFuture =
        Error.Validation("transfer.schedule_must_be_in_future", "A data do agendamento deve ser futura.");

    public static readonly Error NotCancellable =
        Error.Conflict("transfer.not_cancellable", "Somente transferências agendadas podem ser canceladas.");

    public static readonly Error IdempotencyKeyReused =
        Error.Conflict("transfer.idempotency_key_reused", "Esta chave de idempotência já foi usada com dados diferentes.");

    public static readonly Error SourceAccountNotActive =
        Error.BusinessRule("transfer.source_account_not_active", "A conta de origem não está ativa e não pode agendar transferências.");

    public static readonly Error DestinationAccountNotActive =
        Error.BusinessRule("transfer.destination_account_not_active", "A conta de destino não está ativa e não pode receber transferências.");

    public static Error NotFound(Guid transferId) =>
        Error.NotFound("transfer.not_found", $"Transferência {transferId} não encontrada.");
}
