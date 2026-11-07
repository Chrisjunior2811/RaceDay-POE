using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile and performance history.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public UsersController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    /// <remarks>Role required: any logged-in user.</remarks>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var id = User.GetUserId();
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(user.ToDto());
    }

    /// <summary>Updates the logged-in user's own profile (full name and phone number).</summary>
    /// <remarks>Role required: any logged-in user.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var id = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null)
        {
            return Unauthorized();
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();

        return Ok(user.ToDto());
    }

    /// <summary>Returns the logged-in participant's personal performance history (their past results).</summary>
    /// <remarks>Role required: Participant.</remarks>
    /// <response code="200">Array of the participant's results, newest event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">The participant has no results yet.</response>
    [HttpGet("me/history")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultDto>>> GetMyHistory()
    {
        var id = User.GetUserId();
        var results = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.ParticipantId == id)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(Mappers.ResultProjection)
            .ToListAsync();

        await results.AddTotalFinishersAsync(_db);

        if (results.Count == 0)
        {
            return Problem(title: "No history", detail: "You do not have any results yet.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(results);
    }

    /// <summary>Uploads (or replaces) the logged-in user's profile picture, stored in Azure Blob Storage.</summary>
    /// <remarks>
    /// Role required: any logged-in user. Send the image as multipart/form-data in a field named "file".
    /// JPG, PNG or WebP, up to 5 MB.
    /// </remarks>
    /// <response code="200">Picture saved.</response>
    /// <response code="400">No file, wrong type, or file too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="503">Blob storage is not available.</response>
    [HttpPost("me/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ImageUploadResponse>> UploadPicture(IFormFile file)
    {
        var id = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null)
        {
            return Unauthorized();
        }

        var (contentType, error) = await ImageValidator.ValidateAsync(file);
        if (contentType is null)
        {
            return Problem(title: "Invalid image", detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        var blobName = $"users/{id}/{Guid.NewGuid():N}{ImageValidator.ExtensionFor(contentType)}";
        var oldBlobName = user.ProfilePictureBlobName;

        try
        {
            await using var stream = file.OpenReadStream();
            await _blobs.UploadAsync(blobName, stream, contentType);
        }
        catch (StorageUnavailableException)
        {
            return StorageProblem();
        }

        user.ProfilePictureBlobName = blobName;
        await _db.SaveChangesAsync();

        if (oldBlobName is not null)
        {
            try
            {
                await _blobs.DeleteAsync(oldBlobName);
            }
            catch (StorageUnavailableException)
            {
                // The old picture is left behind in storage; the profile already points at the new one.
            }
        }

        return Ok(new ImageUploadResponse { Message = "Profile picture uploaded.", HasImage = true });
    }

    /// <summary>Returns the logged-in user's profile picture, streamed from Azure Blob Storage.</summary>
    /// <remarks>Role required: any logged-in user.</remarks>
    /// <response code="200">The image file.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The user has no profile picture.</response>
    /// <response code="503">Blob storage is not available.</response>
    [HttpGet("me/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetPicture()
    {
        var id = User.GetUserId();
        var blobName = await _db.Users.AsNoTracking()
            .Where(u => u.UserId == id)
            .Select(u => u.ProfilePictureBlobName)
            .FirstOrDefaultAsync();

        if (blobName is null)
        {
            return Problem(title: "No picture", detail: "You have not uploaded a profile picture.",
                statusCode: StatusCodes.Status404NotFound);
        }

        try
        {
            var download = await _blobs.DownloadAsync(blobName);
            if (download is null)
            {
                return Problem(title: "No picture", detail: "You have not uploaded a profile picture.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return File(download.Content, download.ContentType);
        }
        catch (StorageUnavailableException)
        {
            return StorageProblem();
        }
    }

    private ObjectResult StorageProblem() =>
        Problem(title: "Image storage unavailable", detail: "Image storage is not available right now. Please try again later.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
}
