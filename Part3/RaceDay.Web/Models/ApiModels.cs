namespace RaceDay.Web.Models;

// These classes mirror the JSON the Part 2 API sends and receives.
// They hold data only: there is no business logic in the web app.

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasProfilePicture { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class RouteDto
{
    public int RouteId { get; set; }
    public string? RouteDescription { get; set; }
    public decimal DistanceKm { get; set; }
    public int? ElevationGainM { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
    public string? MapUrl { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DistanceKm { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public bool HasBanner { get; set; }
}

public class EventDetailDto : EventDto
{
    public RouteDto? Route { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

public class OrganiserEventDto : EventDto
{
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
}

public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string BibNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public bool HasResult { get; set; }
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string BibNumber { get; set; } = string.Empty;
    public int? FinishTimeSeconds { get; set; }
    public int? FinishPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalFinishers { get; set; }
    public string? FinishTime { get; set; }
}

// ---------- Requests sent to the API ----------

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? OrganiserCode { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}

public class RouteRequest
{
    public decimal DistanceKm { get; set; }
    public string? RouteDescription { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
}

public class CreateEventRequest
{
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RouteRequest Route { get; set; } = new();
}

public class UpdateEventRequest
{
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? EventType { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? RouteDescription { get; set; }
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
}

public class CategoryRequest
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
}

public class UpdateEnrolmentStatusRequest
{
    public string PaymentStatus { get; set; } = string.Empty;
}

public class CaptureResultRequest
{
    public int? FinishTimeSeconds { get; set; }
    public int? FinishPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "Finished";
}
