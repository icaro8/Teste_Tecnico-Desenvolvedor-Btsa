using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Transfers.Application.Abstractions;
using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;

namespace Transfers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(provider =>
        {
            var rules = provider.GetRequiredService<IOptions<TransferRulesOptions>>().Value;
            return new TransferPeriodSchedule(
                rules.DaytimeStart,
                rules.NighttimeStart,
                TimeZoneInfo.FindSystemTimeZoneById(rules.TimeZone));
        });
        services.AddSingleton<DisplayFormatter>();
        services.AddSingleton<AccountPresenter>();
        services.AddSingleton<TransferPresenter>();

        services.AddScoped<TransferSettlement>();
        services.AddScoped<IdempotencyGuard>();

        services.AddScoped<ICommandHandler<OpenAccountCommand, AccountResponse>, OpenAccountHandler>();
        services.AddScoped<ICommandHandler<ChangeOverdraftLimitCommand, AccountResponse>, ChangeOverdraftLimitHandler>();
        services.AddScoped<ICommandHandler<ChangeTransferLimitsCommand, AccountDetailsResponse>, ChangeTransferLimitsHandler>();
        services.AddScoped<ICommandHandler<ChangeAccountStatusCommand, AccountResponse>, ChangeAccountStatusHandler>();
        services.AddScoped<IQueryHandler<GetAccountQuery, AccountDetailsResponse>, GetAccountHandler>();
        services.AddScoped<IQueryHandler<ListAccountsQuery, PagedResponse<AccountResponse>>, ListAccountsHandler>();

        services.AddScoped<ICommandHandler<ExecuteTransferCommand, TransferOutcome>, ExecuteTransferHandler>();
        services.AddScoped<ICommandHandler<ScheduleTransferCommand, TransferOutcome>, ScheduleTransferHandler>();
        services.AddScoped<ICommandHandler<CancelTransferCommand, TransferResponse>, CancelTransferHandler>();
        services.AddScoped<ICommandHandler<ProcessDueTransferCommand, bool>, ProcessDueTransferHandler>();
        services.AddScoped<IQueryHandler<GetTransferQuery, TransferResponse>, GetTransferHandler>();
        services.AddScoped<IQueryHandler<ListTransfersQuery, PagedResponse<TransferResponse>>, ListTransfersHandler>();

        services.AddScoped<TransferEventLogger>();
        services.AddScoped<IDomainEventHandler<TransferScheduled>>(provider => provider.GetRequiredService<TransferEventLogger>());
        services.AddScoped<IDomainEventHandler<TransferCompleted>>(provider => provider.GetRequiredService<TransferEventLogger>());
        services.AddScoped<IDomainEventHandler<TransferFailed>>(provider => provider.GetRequiredService<TransferEventLogger>());
        services.AddScoped<IDomainEventHandler<TransferCancelled>>(provider => provider.GetRequiredService<TransferEventLogger>());

        return services;
    }
}
