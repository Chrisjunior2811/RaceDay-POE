namespace RaceDay.Api.Models;

/// <summary>RouteInfo table: one-to-one with Events.</summary>
public class RouteInfo
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public string? RouteDescription { get; set; }
    public decimal DistanceKm { get; set; }
    public int? ElevationGainM { get; set; } = 0;
    public string? StartPoint { get; set; }
    public string? EndPoint { get; set; }
    public string? MapUrl { get; set; }

    public Event Event { get; set; } = null!;
}
