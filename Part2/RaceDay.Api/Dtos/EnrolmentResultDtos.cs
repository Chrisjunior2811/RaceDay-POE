using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

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
}

public class CaptureResultRequest
{
    [Range(1, 1000000)]
    public int? FinishTimeSeconds { get; set; }

    [Range(1, 1000000)]
    public int? FinishPosition { get; set; }

    [Range(1, 1000000)]
    public int? CategoryPosition { get; set; }

    /// <summary>Finished, DNF or DSQ. Defaults to Finished.</summary>
    [StringLength(20)]
    public string Status { get; set; } = "Finished";
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

    /// <summary>FinishTimeSeconds formatted as hh:mm:ss.</summary>
    public string? FinishTime =>
        FinishTimeSeconds.HasValue
            ? TimeSpan.FromSeconds(FinishTimeSeconds.Value).ToString(@"hh\:mm\:ss")
            : null;
}
