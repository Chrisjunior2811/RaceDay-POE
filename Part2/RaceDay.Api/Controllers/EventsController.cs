using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events (public) and manage events (Organisers).</summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all upcoming events so participants can browse.</summary>
    /// <remarks>Role required: none (public). Only events dated today or later are returned, soonest first.</remarks>
    /// <response code="200">Array of upcoming events.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventDto>>> GetUpcoming()
    {
        var now = DateTime.Now;
        var events = await _db.Events.AsNoTracking()
            .Where(e => e.EventDate >= now)
            .OrderBy(e => e.EventDate)
            .Select(Mappers.EventProjection)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Returns full details for one event, including its route and categories.</summary>
    /// <remarks>Role required: none (public).</remarks>
    /// <response code="200">The event details.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetById(int id)
    {
        var evt = await LoadDetailAsync(id);
        if (evt is null)
        {
            return Problem(title: "Event not found", detail: $"No event with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(evt.ToDetailDto());
    }

    /// <summary>Creates a new event (with its route and distance) owned by the logged-in organiser.</summary>
    /// <remarks>Role required: Organiser. EventType must be Running, Walking or Cycling and the date must be in the future.</remarks>
    /// <response code="201">Event created. Returns the event details.</response>
    /// <response code="400">Validation failed (bad event type, past date, missing route distance...).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDetailDto>> Create([FromBody] CreateEventRequest request)
    {
        var eventType = EventTypes.Normalise(request.EventType);
        if (eventType is null)
        {
            return Problem(title: "Invalid event type", detail: "EventType must be Running, Walking or Cycling.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (request.EventDate <= DateTime.Now)
        {
            return Problem(title: "Invalid date", detail: "EventDate must be in the future.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var evt = new Event
        {
            OrganiserId = User.GetUserId(),
            EventName = request.EventName.Trim(),
            EventType = eventType,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Description = request.Description,
            CreatedAt = DateTime.Now,
            Route = new RouteInfo
            {
                DistanceKm = request.Route.DistanceKm,
                RouteDescription = request.Route.RouteDescription,
                ElevationGainM = request.Route.ElevationGainM ?? 0,
                StartPoint = request.Route.StartPoint,
                EndPoint = request.Route.EndPoint,
                MapUrl = request.Route.MapUrl
            }
        };

        _db.Events.Add(evt);
        await _db.SaveChangesAsync();

        var created = await LoadDetailAsync(evt.EventId);
        return CreatedAtAction(nameof(GetById), new { id = evt.EventId }, created!.ToDetailDto());
    }

    /// <summary>Updates an event owned by the logged-in organiser.</summary>
    /// <remarks>Role required: Organiser (and must own the event).</remarks>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var evt = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (evt is null)
        {
            return Problem(title: "Event not found", detail: $"No event with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        if (evt.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        if (request.EventDate <= DateTime.Now)
        {
            return Problem(title: "Invalid date", detail: "EventDate must be in the future.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        evt.EventName = request.EventName.Trim();
        evt.EventDate = request.EventDate;
        evt.Location = request.Location.Trim();
        evt.Description = request.Description;
        await _db.SaveChangesAsync();

        var updated = await LoadDetailAsync(id);
        return Ok(updated!.ToDetailDto());
    }

    /// <summary>Deletes an event owned by the logged-in organiser (together with its route and categories).</summary>
    /// <remarks>Role required: Organiser (and must own the event). An event that already has enrolments cannot be deleted.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="409">The event already has enrolments.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var evt = await _db.Events
            .Include(e => e.Route)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (evt is null)
        {
            return Problem(title: "Event not found", detail: $"No event with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        if (evt.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        if (await _db.EventEnrolments.AnyAsync(en => en.Category.EventId == id))
        {
            return Problem(title: "Event has enrolments",
                detail: "This event already has enrolments and cannot be deleted.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (evt.Route is not null)
        {
            _db.RouteInfos.Remove(evt.Route);
        }

        _db.Categories.RemoveRange(evt.Categories);
        _db.Events.Remove(evt);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private Task<Event?> LoadDetailAsync(int id) =>
        _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Route)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);
}
