using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers: the API client, flash messages and the "session expired" redirect.</summary>
public abstract class BaseController : Controller
{
    protected BaseController(ApiClient api) => Api = api;

    protected ApiClient Api { get; }

    /// <summary>
    /// If the API rejected our token, the session has ended: returns a redirect to the login page.
    /// Returns null when the response was not an authentication failure.
    /// </summary>
    protected IActionResult? SessionExpired(ApiResponse response)
    {
        if (response.Success || response.Status != HttpStatusCode.Unauthorized)
        {
            return null;
        }

        FlashWarning("Your session has expired. Please log in again.");
        return RedirectToAction("Login", "Account");
    }

    protected void FlashSuccess(string message) => TempData["Success"] = message;

    protected void FlashWarning(string message) => TempData["Warning"] = message;

    protected void FlashError(string? message) =>
        TempData["Error"] = string.IsNullOrWhiteSpace(message) ? "Something went wrong. Please try again." : message;
}
