using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser: add distance or age categories to an event.</summary>
[SessionAuthorize(Role = Roles.Organiser)]
[Route("organiser/events/{eventId:int}/categories")]
public class ManageCategoriesController : BaseController
{
    public ManageCategoriesController(ApiClient api) : base(api) { }

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var page = await BuildPageAsync(eventId, new CategoryFormViewModel());
        if (page.Redirect is not null)
        {
            return page.Redirect;
        }

        return View(page.Model);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add(int eventId, [Bind(Prefix = "Form")] CategoryFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var page = await BuildPageAsync(eventId, form);
            return page.Redirect ?? View("Index", page.Model);
        }

        var response = await Api.CreateCategoryAsync(eventId, ToRequest(form));
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            FlashSuccess("The category has been added.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var response = await Api.GetCategoriesAsync(eventId);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        var category = response.Data?.FirstOrDefault(c => c.CategoryId == id);
        if (!response.Success || category is null)
        {
            FlashError(response.Error ?? "That category could not be found.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        ViewData["EventId"] = eventId;
        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel model)
    {
        model.CategoryId = id;
        ViewData["EventId"] = eventId;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.UpdateCategoryAsync(id, ToRequest(model));
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "The category could not be updated.");
            return View(model);
        }

        FlashSuccess("The category has been updated.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var response = await Api.DeleteCategoryAsync(id);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            FlashSuccess("The category has been deleted.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<(CategoriesPageViewModel? Model, IActionResult? Redirect)> BuildPageAsync(int eventId, CategoryFormViewModel form)
    {
        var eventResponse = await Api.GetEventAsync(eventId);
        if (SessionExpired(eventResponse) is { } expired)
        {
            return (null, expired);
        }

        if (!eventResponse.Success || eventResponse.Data is null)
        {
            FlashError(eventResponse.Error);
            return (null, RedirectToAction("Index", "ManageEvents"));
        }

        var categories = await Api.GetCategoriesAsync(eventId);
        return (new CategoriesPageViewModel
        {
            Event = eventResponse.Data,
            Categories = categories.Data ?? new List<CategoryDto>(),
            Form = form
        }, null);
    }

    private static CategoryRequest ToRequest(CategoryFormViewModel form) => new()
    {
        CategoryName = form.CategoryName.Trim(),
        DistanceKm = form.DistanceKm,
        EntryFee = form.EntryFee,
        MaxParticipants = form.MaxParticipants
    };
}
