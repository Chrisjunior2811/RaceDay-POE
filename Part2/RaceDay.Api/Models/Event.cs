namespace RaceDay.Api.Models;

/// <summary>Events table: each event belongs to one Organiser.</summary>
public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public AppUser Organiser { get; set; } = null!;
    public RouteInfo? Route { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
