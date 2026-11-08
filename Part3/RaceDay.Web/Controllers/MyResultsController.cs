using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Participants: their personal race history.</summary>
[SessionAuthorize(Role = Roles.Participant)]
public class MyResultsController : BaseController
{
    public MyResultsController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await Api.GetMyHistoryAsync();
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        // The API answers 404 when there are no results yet: that is an empty list, not an error
        if (!response.Success && response.Status != HttpStatusCode.NotFound)
        {
            FlashError(response.Error);
        }

        return View(response.Data ?? new List<ResultDto>());
    }
}
