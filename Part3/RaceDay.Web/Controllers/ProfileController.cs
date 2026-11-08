using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Both roles: view and update personal details and upload a profile picture.</summary>
[SessionAuthorize]
public class ProfileController : BaseController
{
    public ProfileController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var response = await Api.GetProfileAsync();
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (!response.Success || response.Data is null)
        {
            FlashError(response.Error);
            return RedirectToAction("Index", "Home");
        }

        return View(ToViewModel(response.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return await ReloadProfileViewAsync(model);
        }

        var response = await Api.UpdateProfileAsync(new UpdateProfileRequest
        {
            FullName = model.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim()
        });

        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (!response.Success || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "Your profile could not be updated.");
            return await ReloadProfileViewAsync(model);
        }

        HttpContext.Session.SetString(SessionKeys.FullName, response.Data.FullName);
        FlashSuccess("Your profile has been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        var problem = ImageRules.Validate(picture);
        if (problem is not null)
        {
            FlashError(problem);
            return RedirectToAction(nameof(Index));
        }

        var response = await Api.UploadProfilePictureAsync(picture!);
        if (SessionExpired(response) is { } expired)
        {
            return expired;
        }

        if (response.Success)
        {
            HttpContext.Session.SetString(SessionKeys.HasPicture, "1");
            FlashSuccess("Your profile picture has been updated.");
        }
        else
        {
            FlashError(response.Error);
        }

        return RedirectToAction(nameof(Index));
    }

    // When validation fails, show the form again with the read-only details filled in
    private async Task<IActionResult> ReloadProfileViewAsync(ProfileViewModel model)
    {
        var current = await Api.GetProfileAsync();
        if (current.Data is not null)
        {
            model.Email = current.Data.Email;
            model.Role = current.Data.Role;
            model.HasPicture = current.Data.HasProfilePicture;
            model.MemberSince = current.Data.CreatedAt;
        }

        return View(nameof(Index), model);
    }

    private static ProfileViewModel ToViewModel(UserDto user) => new()
    {
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        Email = user.Email,
        Role = user.Role,
        HasPicture = user.HasProfilePicture,
        MemberSince = user.CreatedAt
    };
}
