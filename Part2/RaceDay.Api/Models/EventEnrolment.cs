namespace RaceDay.Api.Models;

/// <summary>EventEnrolments table: links a Participant to a Category (and therefore an Event).</summary>
public class EventEnrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public string BibNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    public string PaymentStatus { get; set; } = "Pending";

    public AppUser Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public RaceResult? Result { get; set; }
}
