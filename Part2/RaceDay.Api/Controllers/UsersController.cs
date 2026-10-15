using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile and performance history.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db) => _db = db;

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

        if (results.Count == 0)
        {
            return Problem(title: "No history", detail: "You do not have any results yet.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(results);
    }
}
