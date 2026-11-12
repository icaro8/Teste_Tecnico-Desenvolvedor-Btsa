using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Transfers.Api.Http;

internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = Classify(exception);
        if (status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandled(exception);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }

    private static (int Status, string Title, string Detail) Classify(Exception exception) => exception switch
    {
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflito de concorrência", "O registro foi alterado por outra operação. Tente novamente."),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            (StatusCodes.Status409Conflict, "Conflito", "Já existe um registro com estes dados."),
        _ => (StatusCodes.Status500InternalServerError, "Erro inesperado", "Ocorreu um erro inesperado. Tente novamente mais tarde.")
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception")]
    private partial void LogUnhandled(Exception exception);
}
