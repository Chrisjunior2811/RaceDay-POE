using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RouteRequest
{
    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    public string? RouteDescription { get; set; }

    [Range(0, 10000)]
    public int? ElevationGainM { get; set; }

    [StringLength(150)]
    public string? StartPoint { get; set; }

    [StringLength(150)]
    public string? EndPoint { get; set; }

    [StringLength(255)]
    public string? MapUrl { get; set; }
}

public class CreateEventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>Running, Walking or Cycling.</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Route details, including the event distance in km.</summary>
    [Required]
    public RouteRequest Route { get; set; } = new();
}

public class UpdateEventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class CategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000)]
    public int MaxParticipants { get; set; } = 500;
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
}

public class EventDetailDto : EventDto
{
    public RouteDto? Route { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}
