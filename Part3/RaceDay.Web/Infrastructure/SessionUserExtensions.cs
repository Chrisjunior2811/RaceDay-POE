using RaceDay.Web.Models;

namespace RaceDay.Web.Infrastructure;

public static class SessionKeys
{
    public const string Token = "Token";
    public const string Role = "Role";
    public const string UserId = "UserId";
    public const string FullName = "FullName";
    public const string Email = "Email";
    public const string HasPicture = "HasPicture";
}

/// <summary>Helpers for reading and writing the logged-in user's details in the server-side session.</summary>
public static class SessionUserExtensions
{
    public static bool IsAuthenticated(this ISession session) =>
        !string.IsNullOrEmpty(session.GetString(SessionKeys.Token));

    public static string? UserRole(this ISession session) => session.GetString(SessionKeys.Role);

    public static string UserName(this ISession session) => session.GetString(SessionKeys.FullName) ?? string.Empty;

    public static bool HasPicture(this ISession session) => session.GetString(SessionKeys.HasPicture) == "1";

    public static void SignIn(this ISession session, LoginResponse login)
    {
        session.Clear();
        session.SetString(SessionKeys.Token, login.Token);
        session.SetString(SessionKeys.Role, login.User.Role);
        session.SetString(SessionKeys.UserId, login.User.UserId.ToString());
        session.SetString(SessionKeys.FullName, login.User.FullName);
        session.SetString(SessionKeys.Email, login.User.Email);
        session.SetString(SessionKeys.HasPicture, login.User.HasProfilePicture ? "1" : "0");
    }
}
