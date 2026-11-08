using System.Globalization;

namespace RaceDay.Web.Infrastructure;

public static class TimeParser
{
    /// <summary>Reads a finish time typed as hh:mm:ss (or mm:ss) and converts it to seconds.</summary>
    public static bool TryParseSeconds(string? text, out int seconds)
    {
        seconds = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Trim().Split(':');
        if (parts.Length < 2 || parts.Length > 3)
        {
            return false;
        }

        var numbers = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
            {
                return false;
            }
        }

        var hours = 0;
        int minutes;
        int secs;
        if (parts.Length == 3)
        {
            hours = numbers[0];
            minutes = numbers[1];
            secs = numbers[2];
        }
        else
        {
            minutes = numbers[0];
            secs = numbers[1];
        }

        if (secs > 59 || (parts.Length == 3 && minutes > 59))
        {
            return false;
        }

        var total = (hours * 3600) + (minutes * 60) + secs;
        if (total <= 0)
        {
            return false;
        }

        seconds = total;
        return true;
    }
}
