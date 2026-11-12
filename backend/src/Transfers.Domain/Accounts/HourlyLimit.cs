using Transfers.Domain.Common;

namespace Transfers.Domain.Accounts;

public sealed record HourlyLimit
{
    private HourlyLimit(decimal maxAmount, int maxAttempts)
    {
        MaxAmount = maxAmount;
        MaxAttempts = maxAttempts;
    }

    public decimal MaxAmount { get; private init; }

    public int MaxAttempts { get; private init; }

    public static Result<HourlyLimit> Create(decimal maxAmount, int maxAttempts)
    {
        if (maxAmount <= 0 || !MoneyRules.HasValidPrecision(maxAmount))
        {
            return AccountErrors.InvalidHourlyAmountLimit;
        }

        if (maxAttempts <= 0)
        {
            return AccountErrors.InvalidHourlyAttemptLimit;
        }

        return new HourlyLimit(maxAmount, maxAttempts);
    }
}
