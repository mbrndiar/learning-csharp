using System.Globalization;

namespace ModelingDataBehaviorPractice;

public static class ReservationDate
{
    public static DateOnly ParseIso(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly day)
            ? day
            : throw new FormatException("Use an ISO calendar date in yyyy-MM-dd format.");
    }
}
