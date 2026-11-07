using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Mapping;

public static class ResultQueryExtensions
{
    /// <summary>Fills in how many participants finished each event, so a result can read "47th out of 312".</summary>
    public static async Task AddTotalFinishersAsync(this List<ResultDto> results, RaceDayDbContext db)
    {
        if (results.Count == 0)
        {
            return;
        }

        var eventIds = results.Select(r => r.EventId).Distinct().ToList();

        var counts = await db.Results.AsNoTracking()
            .Where(r => r.Status == ResultStatuses.Finished && eventIds.Contains(r.Enrolment.Category.EventId))
            .GroupBy(r => r.Enrolment.Category.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var result in results)
        {
            result.TotalFinishers = counts.FirstOrDefault(c => c.EventId == result.EventId)?.Count ?? 0;
        }
    }
}
