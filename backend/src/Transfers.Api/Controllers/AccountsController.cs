using Microsoft.AspNetCore.Mvc;
using Transfers.Api.Contracts;
using Transfers.Api.Http;
using Transfers.Application.Abstractions;
using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;

namespace Transfers.Api.Controllers;

[Route("api/accounts")]
public sealed class AccountsController : ApiControllerBase
{
    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta uma conta, seus limites e o uso na última hora")]
    [ProducesResponseType<AccountDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid id,
        [FromServices] IQueryHandler<GetAccountQuery, AccountDetailsResponse> handler,
        CancellationToken cancellationToken) =>
        Respond(await handler.HandleAsync(new GetAccountQuery(id), cancellationToken));

    [HttpGet]
    [EndpointSummary("Lista contas com busca por nome ou CPF, ordenação e paginação")]
    [ProducesResponseType<PagedResponse<AccountResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] IQueryHandler<ListAccountsQuery, PagedResponse<AccountResponse>> handler,
        CancellationToken cancellationToken,
        [FromQuery] AccountSortField sortBy = AccountSortField.HolderName,
        [FromQuery] SortDirection sortDirection = SortDirection.Asc)
    {
        var query = new ListAccountsQuery(search, sortBy, sortDirection, new PageRequest(page, pageSize));
        return Respond(await handler.HandleAsync(query, cancellationToken));
    }

    [HttpPost]
    [EndpointSummary("Abre uma conta")]
    [EndpointDescription("Os limites por hora são opcionais; quando omitidos, valem os limites padrão configurados.")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Open(
        [FromBody] OpenAccountRequest request,
        [FromServices] ICommandHandler<OpenAccountCommand, AccountResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new OpenAccountCommand(
            request.HolderName!,
            request.HolderDocument!,
            request.InitialBalance,
            request.OverdraftLimit,
            ToOptions(request.DaytimeLimit),
            ToOptions(request.NighttimeLimit));

        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error!);
    }

    [HttpPut("{id:guid}/overdraft-limit")]
    [EndpointSummary("Altera o limite de cheque especial")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeOverdraftLimit(
        Guid id,
        [FromBody] ChangeOverdraftLimitRequest request,
        [FromServices] ICommandHandler<ChangeOverdraftLimitCommand, AccountResponse> handler,
        CancellationToken cancellationToken) =>
        Respond(await handler.HandleAsync(new ChangeOverdraftLimitCommand(id, request.OverdraftLimit!.Value), cancellationToken));

    [HttpPut("{id:guid}/transfer-limits")]
    [EndpointSummary("Altera os limites de transferência por hora (diurno e noturno)")]
    [EndpointDescription("Valem para as próximas tentativas, inclusive agendamentos executados depois da alteração.")]
    [ProducesResponseType<AccountDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTransferLimits(
        Guid id,
        [FromBody] ChangeTransferLimitsRequest request,
        [FromServices] ICommandHandler<ChangeTransferLimitsCommand, AccountDetailsResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new ChangeTransferLimitsCommand(id, ToOptions(request.DaytimeLimit)!, ToOptions(request.NighttimeLimit)!);
        return Respond(await handler.HandleAsync(command, cancellationToken));
    }

    [HttpPost("{id:guid}/block")]
    [EndpointSummary("Bloqueia a conta")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Block(
        Guid id,
        [FromServices] ICommandHandler<ChangeAccountStatusCommand, AccountResponse> handler,
        CancellationToken cancellationToken) =>
        ChangeStatus(id, AccountStatus.Blocked, handler, cancellationToken);

    [HttpPost("{id:guid}/deactivate")]
    [EndpointSummary("Inativa a conta")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Deactivate(
        Guid id,
        [FromServices] ICommandHandler<ChangeAccountStatusCommand, AccountResponse> handler,
        CancellationToken cancellationToken) =>
        ChangeStatus(id, AccountStatus.Inactive, handler, cancellationToken);

    [HttpPost("{id:guid}/activate")]
    [EndpointSummary("Reativa a conta")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Activate(
        Guid id,
        [FromServices] ICommandHandler<ChangeAccountStatusCommand, AccountResponse> handler,
        CancellationToken cancellationToken) =>
        ChangeStatus(id, AccountStatus.Active, handler, cancellationToken);

    private async Task<IActionResult> ChangeStatus(
        Guid id,
        AccountStatus status,
        ICommandHandler<ChangeAccountStatusCommand, AccountResponse> handler,
        CancellationToken cancellationToken) =>
        Respond(await handler.HandleAsync(new ChangeAccountStatusCommand(id, status), cancellationToken));

    private static HourlyLimitOptions? ToOptions(HourlyLimitRequest? request) =>
        request is null ? null : new HourlyLimitOptions { MaxAmount = request.MaxAmount, MaxAttempts = request.MaxAttempts };
}
