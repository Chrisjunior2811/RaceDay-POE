using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser dashboard: a summary of every event the organiser has created.</summary>
[SessionAuthorize(Role = Roles.Organiser)]
[Route("organiser")]
public class OrganiserController : BaseController
{
    public OrganiserController(ApiClient api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Dashboard()
    {
        var response = await Api.GetMyEventsAsync();
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (!response.Success)
        {
            FlashError(response.Error);
        }

        var events = response.Data ?? new List<OrganiserEventDto>();
        var now = DateTime.Now;

        return View(new DashboardViewModel
        {
            Events = events,
            TotalEvents = events.Count,
            UpcomingEvents = events.Count(e => e.EventDate >= now),
            TotalEnrolments = events.Sum(e => e.EnrolmentCount),
            NextEvent = events.Where(e => e.EventDate >= now).OrderBy(e => e.EventDate).FirstOrDefault()
        });
    }
}
