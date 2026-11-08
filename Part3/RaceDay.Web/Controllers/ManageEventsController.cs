using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: create, edit and delete events (including the banner image).</summary>
[SessionAuthorize(Role = Roles.Organiser)]
[Route("organiser/events")]
public class ManageEventsController : BaseController
{
    public ManageEventsController(ApiClient api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Index()
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

        return View(response.Data ?? new List<OrganiserEventDto>());
    }

    [HttpGet("create")]
    public IActionResult Create() =>
        View("Form", new EventFormViewModel { EventDate = DateTime.Now.Date.AddDays(30).AddHours(7) });

    [HttpPost("create")]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateBanner(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var response = await Api.CreateEventAsync(new CreateEventRequest
        {
            EventName = model.EventName.Trim(),
            EventType = model.EventType,
            EventDate = model.EventDate,
            Location = model.Location.Trim(),
            Description = model.Description.Trim(),
            Route = new RouteRequest
            {
                DistanceKm = model.DistanceKm,
                RouteDescription = model.RouteDescription,
                StartPoint = model.StartPoint,
                EndPoint = model.EndPoint
            }
        });

        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (!response.Success || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The event could not be created.");
            return View("Form", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var upload = await Api.UploadEventBannerAsync(response.Data.EventId, model.Banner);
            if (!upload.Success)
            {
                FlashWarning($"The event was created, but the banner could not be uploaded: {upload.Error}");
                return RedirectToAction(nameof(Index));
            }
        }

        FlashSuccess("Your event has been created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var response = await Api.GetEventAsync(id);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (!response.Success || response.Data is null)
        {
            FlashError(response.Error);
            return RedirectToAction(nameof(Index));
        }

        var evt = response.Data;
        return View("Form", new EventFormViewModel
        {
            EventId = evt.EventId,
            EventName = evt.EventName,
            EventType = evt.EventType,
            Description = evt.Description ?? string.Empty,
            EventDate = evt.EventDate,
            Location = evt.Location,
            DistanceKm = evt.DistanceKm ?? 0,
            StartPoint = evt.Route?.StartPoint,
            EndPoint = evt.Route?.EndPoint,
            RouteDescription = evt.Route?.RouteDescription,
            HasBanner = evt.HasBanner
        });
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        ValidateBanner(model);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var response = await Api.UpdateEventAsync(id, new UpdateEventRequest
        {
            EventName = model.EventName.Trim(),
            EventType = model.EventType,
            EventDate = model.EventDate,
            Location = model.Location.Trim(),
            Description = model.Description.Trim(),
            DistanceKm = model.DistanceKm,
            RouteDescription = model.RouteDescription,
            StartPoint = model.StartPoint,
            EndPoint = model.EndPoint
        });

        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The event could not be updated.");
            return View("Form", model);
        }

        if (model.Banner is { Length: > 0 })
        {
            var upload = await Api.UploadEventBannerAsync(id, model.Banner);
            if (!upload.Success)
            {
                FlashWarning($"The event was updated, but the banner could not be uploaded: {upload.Error}");
                return RedirectToAction(nameof(Index));
            }
        }

        FlashSuccess("Your event has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await Api.DeleteEventAsync(id);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            FlashSuccess("The event has been deleted.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidateBanner(EventFormViewModel model)
    {
        if (model.Banner is { Length: > 0 })
        {
            var problem = ImageRules.Validate(model.Banner);
            if (problem is not null)
            {
                ModelState.AddModelError(nameof(model.Banner), problem);
            }
        }
    }
}
