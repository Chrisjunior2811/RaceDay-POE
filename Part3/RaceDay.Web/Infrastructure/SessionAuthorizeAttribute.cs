using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Protects a controller or action using the session.
/// Not logged in: redirected to the login page. Logged in with the wrong role: redirected to Access Denied.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>Optional. When set, only users with this role may continue.</summary>
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsAuthenticated())
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl, reason = "login" });
            return;
        }

        if (Role is not null && !string.Equals(session.UserRole(), Role, StringComparison.Ordinal))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
