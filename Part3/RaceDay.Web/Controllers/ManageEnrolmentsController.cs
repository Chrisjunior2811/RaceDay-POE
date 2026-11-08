using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: who has enrolled for an event, in which category, and with what status.</summary>
[SessionAuthorize(Role = Roles.Organiser)]
[Route("organiser/events/{eventId:int}/enrolments")]
public class ManageEnrolmentsController : BaseController
{
    public ManageEnrolmentsController(ApiClient api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var eventResponse = await Api.GetEventAsync(eventId);
        if (SessionExpired(eventResponse) is { } expired)
        {
            return expired;
        }

        if (!eventResponse.Success || eventResponse.Data is null)
        {
            FlashError(eventResponse.Error);
            return RedirectToAction("Index", "ManageEvents");
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success)
        {
            FlashError(enrolments.Error);
        }

        return View(new EnrolmentsPageViewModel
        {
            Event = eventResponse.Data,
            Enrolments = enrolments.Data ?? new List<EnrolmentDto>()
        });
    }

    /// <summary>Confirms an enrolment (Paid) or sets it back to Pending.</summary>
    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> SetStatus(int eventId, int id, string status)
    {
        var response = await Api.SetEnrolmentStatusAsync(id, status);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            FlashSuccess(status == "Paid" ? "The enrolment is now confirmed." : "The enrolment is now pending.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost("{id:int}/remove")]
    public async Task<IActionResult> Remove(int eventId, int id)
    {
        var response = await Api.CancelEnrolmentAsync(id);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            FlashSuccess("The enrolment has been removed.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}
