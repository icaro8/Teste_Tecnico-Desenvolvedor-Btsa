using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Common;

namespace Transfers.Application.Accounts;

internal static class TransferLimitPolicyFactory
{
    public static Result<TransferLimitPolicy> Create(HourlyLimitOptions daytimeOptions, HourlyLimitOptions nighttimeOptions)
    {
        var daytime = HourlyLimit.Create(daytimeOptions.MaxAmount, daytimeOptions.MaxAttempts);
        if (daytime.IsFailure)
        {
            return daytime.Error!;
        }

        var nighttime = HourlyLimit.Create(nighttimeOptions.MaxAmount, nighttimeOptions.MaxAttempts);
        if (nighttime.IsFailure)
        {
            return nighttime.Error!;
        }

        return new TransferLimitPolicy(daytime.Value, nighttime.Value);
    }
}
