using Microsoft.AspNetCore.Mvc;
using Transfers.Application.Transfers;
using Transfers.Domain.Common;

namespace Transfers.Api.Http;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string IdempotentReplayHeader = "Idempotent-Replayed";

    protected IActionResult Respond<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Problem(result.Error!);

    protected IActionResult Problem(Error error) => Problem(
        statusCode: StatusCodeOf(error.Kind),
        title: TitleOf(error.Kind),
        detail: error.Message,
        type: error.Code);

    protected IActionResult RespondCreated(Result<TransferOutcome> result)
    {
        if (result.IsFailure)
        {
            return Problem(result.Error!);
        }

        var outcome = result.Value;
        if (outcome.IsReplay)
        {
            Response.Headers[IdempotentReplayHeader] = "true";
        }

        if (outcome.IsRejected)
        {
            return Rejected(outcome.Transfer);
        }

        return outcome.IsReplay
            ? Ok(outcome.Transfer)
            : CreatedAtAction("Get", "Transfers", new { id = outcome.Transfer.Id }, outcome.Transfer);
    }

    private ObjectResult Rejected(TransferResponse transfer)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext,
            StatusCodes.Status422UnprocessableEntity,
            "Transferência recusada",
            $"transfer.rejected.{transfer.FailureReason}",
            transfer.FailureMessage);

        problem.Extensions["transfer"] = transfer;
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }

    private static int StatusCodeOf(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleOf(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => "Dados inválidos",
        ErrorKind.NotFound => "Recurso não encontrado",
        ErrorKind.Conflict => "Conflito",
        ErrorKind.BusinessRule => "Regra de negócio violada",
        _ => "Erro inesperado"
    };
}
