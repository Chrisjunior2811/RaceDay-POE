using System.Globalization;

namespace RaceDay.Web.Infrastructure;

/// <summary>Display helpers used by the views so dates, times and positions look the same on every page.</summary>
public static class Format
{
    public static string ShortDate(DateTime value) =>
        value.ToString("ddd, d MMM yyyy", CultureInfo.InvariantCulture);

    public static string DateAndTime(DateTime value) =>
        value.ToString("ddd, d MMM yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>1 becomes 1st, 2 becomes 2nd, 47 becomes 47th.</summary>
    public static string Ordinal(int number)
    {
        var suffix = (number % 100) is >= 11 and <= 13
            ? "th"
            : (number % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };

        return number.ToString(CultureInfo.InvariantCulture) + suffix;
    }

    public static string Duration(int? seconds)
    {
        if (!seconds.HasValue)
        {
            return "-";
        }

        var time = TimeSpan.FromSeconds(seconds.Value);
        return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }

    public static string DaysUntil(DateTime date)
    {
        var days = (date.Date - DateTime.Now.Date).Days;
        return days switch
        {
            < 0 => "Past event",
            0 => "Today",
            1 => "Tomorrow",
            _ => $"In {days} days"
        };
    }

    /// <summary>Formats an amount as South African rand, e.g. R250.00.</summary>
    public static string Rand(decimal amount) =>
        "R" + amount.ToString("N2", CultureInfo.InvariantCulture);

    public static string TypeIcon(string eventType) => eventType switch
    {
        "Cycling" => "bi-bicycle",
        "Walking" => "bi-person-walking",
        _ => "bi-lightning-charge-fill"
    };
}
