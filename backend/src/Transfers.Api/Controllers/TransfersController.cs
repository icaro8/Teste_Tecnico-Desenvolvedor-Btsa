using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Transfers.Api.Contracts;
using Transfers.Api.Http;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;

namespace Transfers.Api.Controllers;

[Route("api/transfers")]
public sealed class TransfersController : ApiControllerBase
{
    [HttpPost]
    [EndpointSummary("Realiza uma transferência imediata")]
    [EndpointDescription("Debita a origem e credita o destino de forma atômica. Envie o cabeçalho Idempotency-Key para repetir a requisição com segurança.")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Execute(
        [FromBody] ExecuteTransferRequest request,
        [FromHeader(Name = IdempotencyKeyHeader), MaxLength(100)] string? idempotencyKey,
        [FromServices] ICommandHandler<ExecuteTransferCommand, TransferOutcome> handler,
        CancellationToken cancellationToken)
    {
        var command = new ExecuteTransferCommand(request.SourceAccountId!.Value, request.DestinationAccountId!.Value, request.Amount!.Value, idempotencyKey);
        return RespondCreated(await handler.HandleAsync(command, cancellationToken));
    }

    [HttpPost("scheduled")]
    [EndpointSummary("Agenda uma transferência para uma data futura")]
    [EndpointDescription("Saldo, cheque especial e limites são verificados novamente no momento da execução.")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Schedule(
        [FromBody] ScheduleTransferRequest request,
        [FromHeader(Name = IdempotencyKeyHeader), MaxLength(100)] string? idempotencyKey,
        [FromServices] ICommandHandler<ScheduleTransferCommand, TransferOutcome> handler,
        CancellationToken cancellationToken)
    {
        var command = new ScheduleTransferCommand(
            request.SourceAccountId!.Value,
            request.DestinationAccountId!.Value,
            request.Amount!.Value,
            request.ScheduledFor!.Value,
            idempotencyKey);

        return RespondCreated(await handler.HandleAsync(command, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    [EndpointSummary("Cancela uma transferência agendada")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] ICommandHandler<CancelTransferCommand, TransferResponse> handler,
        CancellationToken cancellationToken) =>
        Respond(await handler.HandleAsync(new CancelTransferCommand(id), cancellationToken));

    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta o status e os dados de uma transferência")]
    [ProducesResponseType<TransferResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid id,
        [FromServices] IQueryHandler<GetTransferQuery, TransferResponse> handler,
        CancellationToken cancellationToken) =>
        Respond(await handler.HandleAsync(new GetTransferQuery(id), cancellationToken));

    [HttpGet]
    [EndpointSummary("Lista transferências com filtros, ordenação e paginação")]
    [ProducesResponseType<PagedResponse<TransferResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? accountId,
        [FromQuery] TransferStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListTransfersQuery, PagedResponse<TransferResponse>> handler,
        CancellationToken cancellationToken,
        [FromQuery] TransferSortField sortBy = TransferSortField.CreatedAt,
        [FromQuery] SortDirection sortDirection = SortDirection.Desc)
    {
        var query = new ListTransfersQuery(accountId, status, sortBy, sortDirection, new PageRequest(page, pageSize));
        return Respond(await handler.HandleAsync(query, cancellationToken));
    }
}
