using System.Linq.Expressions;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Mapping;

/// <summary>Converts entities to the DTOs returned by the API (never exposes PasswordHash).</summary>
public static class Mappers
{
    public static UserDto ToDto(this AppUser u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        PhoneNumber = u.PhoneNumber,
        CreatedAt = u.CreatedAt
    };

    public static RouteDto ToDto(this RouteInfo r) => new()
    {
        RouteId = r.RouteId,
        RouteDescription = r.RouteDescription,
        DistanceKm = r.DistanceKm,
        ElevationGainM = r.ElevationGainM,
        StartPoint = r.StartPoint,
        EndPoint = r.EndPoint,
        MapUrl = r.MapUrl
    };

    public static CategoryDto ToDto(this Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants
    };

    /// <summary>Requires Organiser, Route and Categories to be loaded (Include).</summary>
    public static EventDetailDto ToDetailDto(this Event e) => new()
    {
        EventId = e.EventId,
        EventName = e.EventName,
        EventType = e.EventType,
        EventDate = e.EventDate,
        Location = e.Location,
        Description = e.Description,
        DistanceKm = e.Route?.DistanceKm,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FullName,
        Route = e.Route?.ToDto(),
        Categories = e.Categories.OrderBy(c => c.DistanceKm).ThenBy(c => c.CategoryName).Select(c => c.ToDto()).ToList()
    };

    // Projections: translated to SQL so only the needed columns are read.
    public static readonly Expression<Func<Event, EventDto>> EventProjection = e => new EventDto
    {
        EventId = e.EventId,
        EventName = e.EventName,
        EventType = e.EventType,
        EventDate = e.EventDate,
        Location = e.Location,
        Description = e.Description,
        DistanceKm = e.Route != null ? e.Route.DistanceKm : (decimal?)null,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FullName
    };

    public static readonly Expression<Func<Category, CategoryDto>> CategoryProjection = c => new CategoryDto
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants
    };

    public static readonly Expression<Func<EventEnrolment, EnrolmentDto>> EnrolmentProjection = en => new EnrolmentDto
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant.FullName,
        CategoryId = en.CategoryId,
        CategoryName = en.Category.CategoryName,
        EventId = en.Category.EventId,
        EventName = en.Category.Event.EventName,
        EventDate = en.Category.Event.EventDate,
        BibNumber = en.BibNumber,
        EnrolmentDate = en.EnrolmentDate,
        PaymentStatus = en.PaymentStatus
    };

    public static readonly Expression<Func<RaceResult, ResultDto>> ResultProjection = r => new ResultDto
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.EventName,
        EventDate = r.Enrolment.Category.Event.EventDate,
        CategoryName = r.Enrolment.Category.CategoryName,
        ParticipantName = r.Enrolment.Participant.FullName,
        BibNumber = r.Enrolment.BibNumber,
        FinishTimeSeconds = r.FinishTimeSeconds,
        FinishPosition = r.FinishPosition,
        CategoryPosition = r.CategoryPosition,
        Status = r.Status
    };
}
