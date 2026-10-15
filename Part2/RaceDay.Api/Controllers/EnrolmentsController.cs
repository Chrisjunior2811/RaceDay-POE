using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Event enrolments: participants enter a category; organisers see who entered.</summary>
[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enrols the logged-in participant into a category and issues a bib number.</summary>
    /// <remarks>
    /// Role required: Participant. The {id} is the category id. No request body is needed.
    /// A participant can enter an event only once (in one category) and cannot enter a full category.
    /// </remarks>
    /// <response code="201">Enrolment created. Returns the enrolment including the bib number.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">Category is full, or the participant is already enrolled in this event.</response>
    [HttpPost("categories/{id:int}/enrol")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(int id)
    {
        var userId = User.GetUserId();

        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return Problem(title: "Category not found", detail: $"No category with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        var alreadyInEvent = await _db.EventEnrolments
            .AnyAsync(en => en.ParticipantId == userId && en.Category.EventId == category.EventId);
        if (alreadyInEvent)
        {
            return Problem(title: "Already enrolled", detail: "You are already enrolled in this event.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var taken = await _db.EventEnrolments.CountAsync(en => en.CategoryId == id);
        if (taken >= category.MaxParticipants)
        {
            return Problem(title: "Category full", detail: "This category has reached its maximum number of participants.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // Bib numbers are always one higher than the highest enrolment id, so they never repeat.
        var nextNumber = (await _db.EventEnrolments.MaxAsync(en => (int?)en.EnrolmentId) ?? 0) + 1;

        var enrolment = new EventEnrolment
        {
            ParticipantId = userId,
            CategoryId = id,
            BibNumber = $"BIB{nextNumber:D5}",
            EnrolmentDate = DateTime.Now,
            PaymentStatus = "Pending"
        };

        _db.EventEnrolments.Add(enrolment);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // e.g. two participants enrolling at the same instant and getting the same bib number
            return Problem(title: "Enrolment failed", detail: "Could not complete the enrolment. Please try again.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var dto = await _db.EventEnrolments.AsNoTracking()
            .Where(en => en.EnrolmentId == enrolment.EnrolmentId)
            .Select(Mappers.EnrolmentProjection)
            .SingleAsync();

        return Created("/api/enrolments/me", dto);
    }

    /// <summary>Lists the logged-in participant's own enrolments.</summary>
    /// <remarks>Role required: Participant.</remarks>
    /// <response code="200">Array of the participant's enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("enrolments/me")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetMine()
    {
        var userId = User.GetUserId();
        var enrolments = await _db.EventEnrolments.AsNoTracking()
            .Where(en => en.ParticipantId == userId)
            .OrderBy(en => en.Category.Event.EventDate)
            .Select(Mappers.EnrolmentProjection)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Lists all enrolments for an event (organiser management view).</summary>
    /// <remarks>Role required: Organiser who owns the event. The {id} is the event id.</remarks>
    /// <response code="200">Array of enrolments for the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{id:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetForEvent(int id)
    {
        var evt = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);
        if (evt is null)
        {
            return Problem(title: "Event not found", detail: $"No event with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        if (evt.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        var enrolments = await _db.EventEnrolments.AsNoTracking()
            .Where(en => en.Category.EventId == id)
            .OrderBy(en => en.BibNumber)
            .Select(Mappers.EnrolmentProjection)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Cancels (removes) an enrolment.</summary>
    /// <remarks>
    /// Role required: the Participant who made the enrolment, or the Organiser who owns the event.
    /// An enrolment that already has a result cannot be cancelled.
    /// </remarks>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The enrolment belongs to someone else, or the event to another organiser.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.EventEnrolments
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .Include(en => en.Result)
            .FirstOrDefaultAsync(en => en.EnrolmentId == id);

        if (enrolment is null)
        {
            return Problem(title: "Enrolment not found", detail: $"No enrolment with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        var userId = User.GetUserId();
        var isOwningParticipant = User.IsInRole(Roles.Participant) && enrolment.ParticipantId == userId;
        var isOwningOrganiser = User.IsInRole(Roles.Organiser) && enrolment.Category.Event.OrganiserId == userId;
        if (!isOwningParticipant && !isOwningOrganiser)
        {
            return Forbid();
        }

        if (enrolment.Result is not null)
        {
            return Problem(title: "Result already captured",
                detail: "This enrolment already has a result and cannot be cancelled.",
                statusCode: StatusCodes.Status409Conflict);
        }

        _db.EventEnrolments.Remove(enrolment);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
