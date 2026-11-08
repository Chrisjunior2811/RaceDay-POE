using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(ApiClient api) : base(api) { }

    /// <summary>
    /// Participants: upcoming events with filters. Organisers are sent to their dashboard.
    /// </summary>
    [SessionAuthorize]
    public async Task<IActionResult> Index(HomeIndexViewModel model)
    {
        if (HttpContext.Session.UserRole() == Roles.Organiser)
        {
            return RedirectToAction("Dashboard", "Organiser");
        }

        var response = await Api.GetUpcomingEventsAsync(model.Type, model.Search, model.MinDistanceKm, model.MaxDistanceKm);
        if (SessionExpired(response) is { } redirect)
        {
            return redirect;
        }

        if (!response.Success)
        {
            FlashError(response.Error);
        }

        model.Events = response.Data ?? new List<EventDto>();
        return View(model);
    }

    /// <summary>Friendly error page: never shows stack traces or raw error text.</summary>
    public IActionResult Error(int? code)
    {
        var model = code switch
        {
            404 => new ErrorViewModel { Code = 404, Title = "Page not found", Message = "We could not find the page you were looking for." },
            403 => new ErrorViewModel { Code = 403, Title = "Access denied", Message = "You do not have permission to open that page." },
            _ => new ErrorViewModel { Code = code ?? 500, Title = "Something went wrong", Message = "Something unexpected happened on our side. Please try again." }
        };

        Response.StatusCode = model.Code is >= 400 and <= 599 ? model.Code : 500;
        return View(model);
    }
}
