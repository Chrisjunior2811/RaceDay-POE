namespace RaceDay.Api.Models;

/// <summary>Categories table: entry categories for an event (e.g. 5km, 10km).</summary>
public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; } = 500;

    public Event Event { get; set; } = null!;
    public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
}
