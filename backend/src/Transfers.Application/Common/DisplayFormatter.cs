using System.Globalization;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Common;

public sealed class DisplayFormatter(TransferPeriodSchedule schedule)
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    public static string Money(decimal amount) => amount.ToString("C", Culture);

    public string? DateTime(DateTimeOffset? instant) => instant is { } value
        ? TimeZoneInfo.ConvertTime(value, schedule.TimeZone).ToString("dd/MM/yyyy HH:mm", Culture)
        : null;

    public static string Document(string digits) => digits.Length == 11
        ? $"{digits[..3]}.{digits[3..6]}.{digits[6..9]}-{digits[9..]}"
        : digits;

    public static string Label(AccountStatus status) => status switch
    {
        AccountStatus.Active => "Ativa",
        AccountStatus.Blocked => "Bloqueada",
        AccountStatus.Inactive => "Inativa",
        _ => status.ToString()
    };

    public static Tone ToneOf(AccountStatus status) => status switch
    {
        AccountStatus.Active => Tone.Success,
        AccountStatus.Blocked => Tone.Danger,
        _ => Tone.Neutral
    };

    public static string Label(TransferStatus status) => status switch
    {
        TransferStatus.Scheduled => "Agendada",
        TransferStatus.Processing => "Em processamento",
        TransferStatus.Completed => "Concluída",
        TransferStatus.Failed => "Falhou",
        TransferStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };

    public static Tone ToneOf(TransferStatus status) => status switch
    {
        TransferStatus.Scheduled => Tone.Info,
        TransferStatus.Processing => Tone.Warning,
        TransferStatus.Completed => Tone.Success,
        TransferStatus.Failed => Tone.Danger,
        _ => Tone.Neutral
    };

    public static string Label(TransferKind kind) => kind switch
    {
        TransferKind.Immediate => "Imediata",
        TransferKind.Scheduled => "Agendada",
        _ => kind.ToString()
    };

    public static string Label(TransferPeriod period) => period switch
    {
        TransferPeriod.Daytime => "Diurno",
        TransferPeriod.Nighttime => "Noturno",
        _ => period.ToString()
    };

    public static string Describe(TransferFailureReason reason) => reason switch
    {
        TransferFailureReason.SourceAccountNotActive => "A conta de origem não está ativa.",
        TransferFailureReason.DestinationAccountNotActive => "A conta de destino não está ativa.",
        TransferFailureReason.InsufficientFunds => "Saldo somado ao cheque especial é insuficiente.",
        TransferFailureReason.HourlyAmountLimitExceeded => "O valor ultrapassa o limite por hora do período atual.",
        TransferFailureReason.HourlyAttemptLimitExceeded => "Limite de tentativas por hora atingido.",
        _ => reason.ToString()
    };
}
