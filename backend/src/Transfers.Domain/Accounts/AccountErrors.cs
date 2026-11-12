using Transfers.Domain.Common;

namespace Transfers.Domain.Accounts;

public static class AccountErrors
{
    public static readonly Error HolderNameRequired =
        Error.Validation("account.holder_name_required", "Informe o nome do titular.");

    public static readonly Error InvalidDocument =
        Error.Validation("account.invalid_document", "Informe um CPF válido.");

    public static readonly Error DocumentAlreadyRegistered =
        Error.Conflict("account.document_already_registered", "Já existe uma conta para este CPF.");

    public static readonly Error InvalidInitialBalance =
        Error.Validation("account.invalid_initial_balance", "O saldo inicial não pode ser negativo e deve ter no máximo 2 casas decimais.");

    public static readonly Error InvalidOverdraftLimit =
        Error.Validation("account.invalid_overdraft_limit", "O limite de cheque especial não pode ser negativo e deve ter no máximo 2 casas decimais.");

    public static readonly Error OverdraftLimitBelowUsage =
        Error.BusinessRule("account.overdraft_limit_below_usage", "O novo limite é menor que o cheque especial já utilizado.");

    public static readonly Error InvalidHourlyAmountLimit =
        Error.Validation("account.invalid_hourly_amount_limit", "O valor máximo por hora deve ser maior que zero e ter no máximo 2 casas decimais.");

    public static readonly Error InvalidHourlyAttemptLimit =
        Error.Validation("account.invalid_hourly_attempt_limit", "O número de tentativas por hora deve ser maior que zero.");

    public static readonly Error AlreadyInStatus =
        Error.Conflict("account.already_in_status", "A conta já está neste status.");

    public static Error NotFound(Guid accountId) =>
        Error.NotFound("account.not_found", $"Conta {accountId} não encontrada.");
}
