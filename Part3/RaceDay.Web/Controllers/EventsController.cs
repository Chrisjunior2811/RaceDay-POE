using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Participants: full event information and enrolling in a category.</summary>
[SessionAuthorize(Role = Roles.Participant)]
public class EventsController : BaseController
{
    public EventsController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var eventResponse = await Api.GetEventAsync(id);
        if (SessionExpired(eventResponse) is { } expired)
        {
            return expired;
        }

        if (!eventResponse.Success || eventResponse.Data is null)
        {
            FlashError(eventResponse.Status == HttpStatusCode.NotFound ? "That event could not be found." : eventResponse.Error);
            return RedirectToAction("Index", "Home");
        }

        var enrolmentsResponse = await Api.GetMyEnrolmentsAsync();
        var existing = enrolmentsResponse.Data?.FirstOrDefault(e => e.EventId == id);

        return View(new EventDetailsViewModel { Event = eventResponse.Data, ExistingEnrolment = existing });
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        if (categoryId <= 0)
        {
            FlashError("Please choose a category before enrolling.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var response = await Api.EnrolAsync(categoryId);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            FlashSuccess($"You are enrolled! Your bib number is {response.Data?.BibNumber}. The organiser will confirm your entry.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}
