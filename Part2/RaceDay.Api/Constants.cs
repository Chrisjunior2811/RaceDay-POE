using System.Security.Claims;

namespace RaceDay.Api;

/// <summary>The two roles in the RaceDay system (stored in Users.Role).</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Allowed values for Events.EventType (matches the CHECK constraint in the Part 1 SQL script).</summary>
public static class EventTypes
{
    public static readonly string[] All = { "Running", "Walking", "Cycling" };

    public static string? Normalise(string? value) =>
        All.FirstOrDefault(t => string.Equals(t, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>Allowed values for Results.Status (matches the CHECK constraint in the Part 1 SQL script).</summary>
public static class ResultStatuses
{
    public const string Finished = "Finished";
    public static readonly string[] All = { "Finished", "DNF", "DSQ" };

    public static string? Normalise(string? value) =>
        All.FirstOrDefault(s => string.Equals(s, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class UserClaimsExtensions
{
    /// <summary>Reads the logged-in user's id from the JWT 'sub' claim.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("sub")?.Value;
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("The authenticated user has no valid 'sub' claim.");
    }
}
