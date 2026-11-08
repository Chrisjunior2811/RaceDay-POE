using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Participants: every event they have enrolled for, with its status.</summary>
[SessionAuthorize(Role = Roles.Participant)]
public class MyEnrolmentsController : BaseController
{
    public MyEnrolmentsController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await Api.GetMyEnrolmentsAsync();
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (!response.Success)
        {
            FlashError(response.Error);
        }

        return View(response.Data ?? new List<EnrolmentDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        var response = await Api.CancelEnrolmentAsync(id);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            FlashSuccess("Your enrolment has been cancelled.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Index));
    }
}
