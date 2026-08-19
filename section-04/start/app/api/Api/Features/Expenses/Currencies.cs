namespace ExpenseTracker.Api.Features.Expenses;

/// <summary>
/// The small allowed set of ISO 4217 currency codes. The frontend mirrors this list
/// (see app/web/src/lib/currency.ts). Extend both to support a new currency.
/// </summary>
public static class Currencies
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
    {
        "EUR",
        "USD",
        "GBP",
        "JPY",
    };

    public static bool IsAllowed(string? code) => code is not null && Allowed.Contains(code);
}
