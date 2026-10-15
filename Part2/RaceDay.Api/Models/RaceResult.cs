namespace RaceDay.Api.Models;

/// <summary>Results table: at most one result per enrolment, captured after race day.</summary>
public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int? FinishTimeSeconds { get; set; }
    public int? FinishPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string Status { get; set; } = "Finished";

    public EventEnrolment Enrolment { get; set; } = null!;
}
