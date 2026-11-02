using System.Globalization;

namespace ControlDeDinero.Application;

internal static class MonthRange
{
    /// <summary>
    /// Turns "2026-11" into [2026-11-01, 2026-12-01). An empty value means "no filter".
    /// </summary>
    public static (DateTime? From, DateTime? To) Parse(string? month)
    {
        if (string.IsNullOrWhiteSpace(month))
            return (null, null);

        if (!DateTime.TryParseExact(month.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var first))
            throw new ValidationException("Month must use the format yyyy-MM, for example 2026-11.");

        return (first, first.AddMonths(1));
    }

    public static bool Contains(DateTime date, DateTime? from, DateTime? to) =>
        (from is null || date >= from.Value) && (to is null || date < to.Value);
}
