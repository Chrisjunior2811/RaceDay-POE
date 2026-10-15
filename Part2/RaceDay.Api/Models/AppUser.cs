namespace RaceDay.Api.Models;

/// <summary>Users table: holds both Organisers and Participants (distinguished by Role).</summary>
public class AppUser
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
}
