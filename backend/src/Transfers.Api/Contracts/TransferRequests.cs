using System.ComponentModel.DataAnnotations;

namespace Transfers.Api.Contracts;

public sealed record ExecuteTransferRequest(
    [Required] Guid? SourceAccountId,
    [Required] Guid? DestinationAccountId,
    [Required] decimal? Amount);

public sealed record ScheduleTransferRequest(
    [Required] Guid? SourceAccountId,
    [Required] Guid? DestinationAccountId,
    [Required] decimal? Amount,
    [Required] DateTimeOffset? ScheduledFor);
