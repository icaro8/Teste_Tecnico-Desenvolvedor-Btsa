namespace Transfers.Domain.Accounts;

public static class Cpf
{
    private const int Length = 11;

    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = new string(input.Where(char.IsAsciiDigit).ToArray());
        return IsValid(digits) ? digits : null;
    }

    private static bool IsValid(string digits)
    {
        if (digits.Length != Length || digits.Distinct().Count() == 1)
        {
            return false;
        }

        return CheckDigit(digits, 9) == digits[9] - '0'
            && CheckDigit(digits, 10) == digits[10] - '0';
    }

    private static int CheckDigit(string digits, int position)
    {
        var sum = 0;
        for (var index = 0; index < position; index++)
        {
            sum += (digits[index] - '0') * (position + 1 - index);
        }

        var remainder = sum % Length;
        return remainder < 2 ? 0 : Length - remainder;
    }
}
