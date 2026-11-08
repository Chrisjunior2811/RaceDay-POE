using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: capture finish times and finishing positions after an event.</summary>
[SessionAuthorize(Role = Roles.Organiser)]
[Route("organiser/events/{eventId:int}/results")]
public class ManageResultsController : BaseController
{
    public ManageResultsController(ApiClient api) : base(api) { }

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
        var results = await Api.GetEventResultsAsync(eventId);
        if (!enrolments.Success)
        {
            FlashError(enrolments.Error);
        }

        return View(new ResultsPageViewModel
        {
            Event = eventResponse.Data,
            Enrolments = enrolments.Data ?? new List<EnrolmentDto>(),
            ResultsByEnrolment = (results.Data ?? new List<ResultDto>()).ToDictionary(r => r.EnrolmentId)
        });
    }

    /// <summary>Saves one participant's result. Results are visible to the participant straight away.</summary>
    [HttpPost("capture")]
    public async Task<IActionResult> Capture(int eventId, ResultEntryViewModel model)
    {
        int? finishSeconds = null;
        if (!string.IsNullOrWhiteSpace(model.FinishTime))
        {
            if (!TimeParser.TryParseSeconds(model.FinishTime, out var seconds))
            {
                FlashError("Please enter the finish time as hh:mm:ss, for example 01:45:30.");
                return RedirectToAction(nameof(Index), new { eventId });
            }

            finishSeconds = seconds;
        }

        if (model.Status == "Finished" && finishSeconds is null)
        {
            FlashError("A finisher needs a finish time, for example 01:45:30.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (!ModelState.IsValid)
        {
            FlashError("Please check the positions: they must be whole numbers of 1 or more.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var response = await Api.CaptureResultAsync(model.EnrolmentId, new CaptureResultRequest
        {
            FinishTimeSeconds = finishSeconds,
            FinishPosition = model.FinishPosition,
            CategoryPosition = model.CategoryPosition,
            Status = model.Status
        });

        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            FlashSuccess("The result has been saved and is now visible to the participant.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}
