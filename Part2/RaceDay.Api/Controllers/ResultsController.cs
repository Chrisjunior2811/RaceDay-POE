using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Race results: captured by organisers, viewable by everyone.</summary>
[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Captures a race result (finish time and positions) against a participant's enrolment.</summary>
    /// <remarks>
    /// Role required: Organiser who owns the event. The {id} is the enrolment id.
    /// Status is Finished, DNF or DSQ; a Finished result needs a finish time.
    /// </remarks>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Validation failed (bad status, or Finished without a finish time).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Enrolment does not exist.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("enrolments/{id:int}/result")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Capture(int id, [FromBody] CaptureResultRequest request)
    {
        var enrolment = await _db.EventEnrolments
            .Include(en => en.Category).ThenInclude(c => c.Event)
            .Include(en => en.Result)
            .FirstOrDefaultAsync(en => en.EnrolmentId == id);

        if (enrolment is null)
        {
            return Problem(title: "Enrolment not found", detail: $"No enrolment with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        if (enrolment.Category.Event.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        var status = ResultStatuses.Normalise(request.Status);
        if (status is null)
        {
            return Problem(title: "Invalid status", detail: "Status must be Finished, DNF or DSQ.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (status == ResultStatuses.Finished && request.FinishTimeSeconds is null)
        {
            return Problem(title: "Finish time required", detail: "A Finished result needs FinishTimeSeconds.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (enrolment.Result is not null)
        {
            return Problem(title: "Result already captured",
                detail: "A result has already been captured for this enrolment.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var result = new RaceResult
        {
            EnrolmentId = id,
            FinishTimeSeconds = request.FinishTimeSeconds,
            FinishPosition = request.FinishPosition,
            CategoryPosition = request.CategoryPosition,
            Status = status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var dto = await _db.Results.AsNoTracking()
            .Where(r => r.ResultId == result.ResultId)
            .Select(Mappers.ResultProjection)
            .SingleAsync();

        return CreatedAtAction(nameof(GetById), new { id = result.ResultId }, dto);
    }

    /// <summary>Returns the full results list for an event.</summary>
    /// <remarks>Role required: none (public). Ordered by category, then finishing position. The {id} is the event id.</remarks>
    /// <response code="200">Array of results.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{id:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ResultDto>>> GetForEvent(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id))
        {
            return Problem(title: "Event not found", detail: $"No event with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        var results = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment.Category.EventId == id)
            .OrderBy(r => r.Enrolment.Category.CategoryName)
            .ThenBy(r => r.CategoryPosition)
            .Select(Mappers.ResultProjection)
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Returns a single result record.</summary>
    /// <remarks>Role required: none (public).</remarks>
    /// <response code="200">The result.</response>
    /// <response code="404">Result does not exist.</response>
    [HttpGet("results/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> GetById(int id)
    {
        var result = await _db.Results.AsNoTracking()
            .Where(r => r.ResultId == id)
            .Select(Mappers.ResultProjection)
            .FirstOrDefaultAsync();

        if (result is null)
        {
            return Problem(title: "Result not found", detail: $"No result with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(result);
    }
}
