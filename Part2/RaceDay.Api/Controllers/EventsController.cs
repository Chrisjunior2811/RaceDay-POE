using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events (public) and manage events (Organisers).</summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public EventsController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    /// <summary>Lists all upcoming events so participants can browse, with optional filters.</summary>
    /// <remarks>
    /// Role required: none (public). Only events dated today or later are returned, soonest first.
    /// Optional query filters: type (Running, Walking or Cycling), search (part of the name or location),
    /// minDistanceKm and maxDistanceKm.
    /// </remarks>
    /// <response code="200">Array of upcoming events.</response>
    /// <response code="400">The event type filter is not valid.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<EventDto>>> GetUpcoming(
        [FromQuery] string? type,
        [FromQuery] string? search,
        [FromQuery] decimal? minDistanceKm,
        [FromQuery] decimal? maxDistanceKm)
    {
        var now = DateTime.Now;
        var query = _db.Events.AsNoTracking().Where(e => e.EventDate >= now);

        if (!string.IsNullOrWhiteSpace(type))
        {
            var eventType = EventTypes.Normalise(type);
            if (eventType is null)
            {
                return Problem(title: "Invalid event type", detail: "type must be Running, Walking or Cycling.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            query = query.Where(e => e.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.EventName.Contains(term) || e.Location.Contains(term));
        }

        if (minDistanceKm.HasValue)
        {
            var min = minDistanceKm.Value;
            query = query.Where(e => e.Route != null && e.Route.DistanceKm >= min);
        }

        if (maxDistanceKm.HasValue)
        {
            var max = maxDistanceKm.Value;
            query = query.Where(e => e.Route != null && e.Route.DistanceKm <= max);
        }

        var events = await query
            .OrderBy(e => e.EventDate)
            .Select(Mappers.EventProjection)
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Lists every event created by the logged-in organiser (past and upcoming) with enrolment counts.</summary>
    /// <remarks>Role required: Organiser. Used by the organiser dashboard.</remarks>
    /// <response code="200">Array of the organiser's events.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<OrganiserEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<OrganiserEventDto>>> GetMine()
    {
        var userId = User.GetUserId();

        var events = await _db.Events.AsNoTracking()
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate)
            .Select(e => new OrganiserEventDto
            {
                EventId = e.EventId,
                EventName = e.EventName,
                EventType = e.EventType,
                EventDate = e.EventDate,
                Location = e.Location,
                Description = e.Description,
                DistanceKm = e.Route != null ? e.Route.DistanceKm : (decimal?)null,
                OrganiserId = e.OrganiserId,
                OrganiserName = e.Organiser.FullName,
                HasBanner = e.BannerBlobName != null,
                CategoryCount = e.Categories.Count(),
                EnrolmentCount = e.Categories.SelectMany(c => c.Enrolments).Count()
            })
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
        var evt = await _db.Events.Include(e => e.Route).FirstOrDefaultAsync(e => e.EventId == id);
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

        string? newType = null;
        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            newType = EventTypes.Normalise(request.EventType);
            if (newType is null)
            {
                return Problem(title: "Invalid event type", detail: "EventType must be Running, Walking or Cycling.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        evt.EventName = request.EventName.Trim();
        evt.EventDate = request.EventDate;
        evt.Location = request.Location.Trim();
        evt.Description = request.Description;

        if (newType is not null)
        {
            evt.EventType = newType;
        }

        if (request.DistanceKm.HasValue)
        {
            if (evt.Route is null)
            {
                evt.Route = new RouteInfo { DistanceKm = request.DistanceKm.Value };
            }
            else
            {
                evt.Route.DistanceKm = request.DistanceKm.Value;
            }
        }

        if (evt.Route is not null)
        {
            if (request.RouteDescription is not null)
            {
                evt.Route.RouteDescription = request.RouteDescription;
            }

            if (request.StartPoint is not null)
            {
                evt.Route.StartPoint = request.StartPoint;
            }

            if (request.EndPoint is not null)
            {
                evt.Route.EndPoint = request.EndPoint;
            }
        }

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

    /// <summary>Uploads (or replaces) the banner image for an event. The image is stored in Azure Blob Storage.</summary>
    /// <remarks>
    /// Role required: Organiser who owns the event. Send the image as multipart/form-data in a field named "file".
    /// JPG, PNG or WebP, up to 5 MB.
    /// </remarks>
    /// <response code="200">Banner saved.</response>
    /// <response code="400">No file, wrong type, or file too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event does not exist.</response>
    /// <response code="503">Blob storage is not available.</response>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = Roles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ImageUploadResponse>> UploadBanner(int id, IFormFile file)
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

        var (contentType, error) = await ImageValidator.ValidateAsync(file);
        if (contentType is null)
        {
            return Problem(title: "Invalid image", detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        var blobName = $"events/{id}/{Guid.NewGuid():N}{ImageValidator.ExtensionFor(contentType)}";
        var oldBlobName = evt.BannerBlobName;

        try
        {
            await using var stream = file.OpenReadStream();
            await _blobs.UploadAsync(blobName, stream, contentType);
        }
        catch (StorageUnavailableException)
        {
            return StorageProblem();
        }

        evt.BannerBlobName = blobName;
        await _db.SaveChangesAsync();

        if (oldBlobName is not null)
        {
            try
            {
                await _blobs.DeleteAsync(oldBlobName);
            }
            catch (StorageUnavailableException)
            {
                // The old image is left behind in storage; the event already points at the new one.
            }
        }

        return Ok(new ImageUploadResponse { Message = "Banner uploaded.", HasImage = true });
    }

    /// <summary>Returns the banner image of an event, streamed from Azure Blob Storage.</summary>
    /// <remarks>Role required: none (public).</remarks>
    /// <response code="200">The image file.</response>
    /// <response code="404">The event has no banner.</response>
    /// <response code="503">Blob storage is not available.</response>
    [HttpGet("{id:int}/banner")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetBanner(int id)
    {
        var blobName = await _db.Events.AsNoTracking()
            .Where(e => e.EventId == id)
            .Select(e => e.BannerBlobName)
            .FirstOrDefaultAsync();

        if (blobName is null)
        {
            return Problem(title: "No banner", detail: "This event has no banner image.", statusCode: StatusCodes.Status404NotFound);
        }

        try
        {
            var download = await _blobs.DownloadAsync(blobName);
            if (download is null)
            {
                return Problem(title: "No banner", detail: "This event has no banner image.", statusCode: StatusCodes.Status404NotFound);
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

    private Task<Event?> LoadDetailAsync(int id) =>
        _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Route)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);
}
