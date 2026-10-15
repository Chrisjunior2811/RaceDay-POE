using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Mapping;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Entry categories for an event (e.g. 5km, 10km, Under 20).</summary>
[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db) => _db = db;

    /// <summary>Lists all entry categories for a specific event.</summary>
    /// <remarks>Role required: none (public). The {id} is the event id.</remarks>
    /// <response code="200">Array of categories.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpGet("events/{id:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryDto>>> GetForEvent(int id)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == id))
        {
            return Problem(title: "Event not found", detail: $"No event with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == id)
            .OrderBy(c => c.DistanceKm).ThenBy(c => c.CategoryName)
            .Select(Mappers.CategoryProjection)
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Adds a new entry category to an event.</summary>
    /// <remarks>Role required: Organiser who owns the event. The {id} is the event id.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another organiser.</response>
    /// <response code="404">Event does not exist.</response>
    [HttpPost("events/{id:int}/categories")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Create(int id, [FromBody] CategoryRequest request)
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

        var category = new Category
        {
            EventId = id,
            CategoryName = request.CategoryName.Trim(),
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{id}/categories", category.ToDto());
    }

    /// <summary>Updates an existing category.</summary>
    /// <remarks>Role required: Organiser who owns the event the category belongs to. The {id} is the category id.</remarks>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the category belongs to another organiser's event.</response>
    /// <response code="404">Category does not exist.</response>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(int id, [FromBody] CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return Problem(title: "Category not found", detail: $"No category with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        if (category.Event.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;
        await _db.SaveChangesAsync();

        return Ok(category.ToDto());
    }

    /// <summary>Deletes a category.</summary>
    /// <remarks>Role required: Organiser who owns the event. A category that already has enrolments cannot be deleted.</remarks>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the category belongs to another organiser's event.</response>
    /// <response code="404">Category does not exist.</response>
    /// <response code="409">The category already has enrolments.</response>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return Problem(title: "Category not found", detail: $"No category with id {id}.", statusCode: StatusCodes.Status404NotFound);
        }

        if (category.Event.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        if (await _db.EventEnrolments.AnyAsync(en => en.CategoryId == id))
        {
            return Problem(title: "Category has enrolments",
                detail: "This category already has enrolments and cannot be deleted.",
                statusCode: StatusCodes.Status409Conflict);
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
