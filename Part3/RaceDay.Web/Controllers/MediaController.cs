using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Serves images to the browser. The web app asks the API for the image (the API reads it from
/// Azure Blob Storage), so the browser and this app never talk to Azure directly.
/// </summary>
[SessionAuthorize]
public class MediaController : BaseController
{
    public MediaController(ApiClient api) : base(api) { }

    [HttpGet("/media/event/{id:int}")]
    public async Task<IActionResult> EventBanner(int id)
    {
        var image = await Api.GetEventBannerAsync(id);
        return image is null ? NotFound() : File(image.Bytes, image.ContentType);
    }

    [HttpGet("/media/me")]
    public async Task<IActionResult> ProfilePicture()
    {
        var image = await Api.GetProfilePictureAsync();
        return image is null ? NotFound() : File(image.Bytes, image.ContentType);
    }
}
