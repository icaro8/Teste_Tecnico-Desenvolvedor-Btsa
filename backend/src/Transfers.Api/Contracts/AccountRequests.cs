using System.ComponentModel.DataAnnotations;

namespace Transfers.Api.Contracts;

public sealed record OpenAccountRequest(
    [Required, MaxLength(150)] string? HolderName,
    [Required, MaxLength(14)] string? HolderDocument,
    decimal InitialBalance,
    decimal OverdraftLimit,
    HourlyLimitRequest? DaytimeLimit,
    HourlyLimitRequest? NighttimeLimit);

public sealed record HourlyLimitRequest(decimal MaxAmount, int MaxAttempts);

public sealed record ChangeTransferLimitsRequest([Required] HourlyLimitRequest? DaytimeLimit, [Required] HourlyLimitRequest? NighttimeLimit);

public sealed record ChangeOverdraftLimitRequest([Required] decimal? OverdraftLimit);
