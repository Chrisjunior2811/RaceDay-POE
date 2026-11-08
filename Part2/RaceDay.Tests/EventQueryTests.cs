using System.Net;
using System.Net.Http.Json;
using RaceDay.Api;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>Event filters, the organiser's own events, partial event updates and finisher totals.</summary>
public class EventQueryTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventQueryTests(RaceDayFactory factory) => _factory = factory;

    private static string Unique(string prefix) => $"{prefix}{Guid.NewGuid():N}";

    private static async Task<EventDetailDto> CreateAsync(HttpClient organiser, string name, string type, decimal distanceKm)
    {
        var request = TestData.NewEventRequest(name);
        request.EventType = type;
        request.Route.DistanceKm = distanceKm;

        var response = await organiser.PostAsJsonAsync("/api/events", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDetailDto>())!;
    }

    [Fact]
    public async Task Filter_ByType_ReturnsOnlyThatType()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var tag = Unique("TypeFilter");
        var run = await CreateAsync(organiser, $"{tag} Run", "Running", 10);
        var ride = await CreateAsync(organiser, $"{tag} Ride", "Cycling", 80);

        var list = await _factory.CreateClient().GetFromJsonAsync<List<EventDto>>($"/api/events?type=Cycling&search={tag}");

        Assert.Contains(list!, e => e.EventId == ride.EventId);
        Assert.DoesNotContain(list!, e => e.EventId == run.EventId);
    }

    [Fact]
    public async Task Filter_ByInvalidType_ReturnsBadRequest()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events?type=Swimming");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Filter_BySearchText_MatchesNameOrLocation()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var tag = Unique("Search");
        var match = await CreateAsync(organiser, $"{tag} Parkrun", "Running", 5);
        await CreateAsync(organiser, Unique("Other"), "Running", 5);

        var list = await _factory.CreateClient().GetFromJsonAsync<List<EventDto>>($"/api/events?search={tag}");

        Assert.Single(list!);
        Assert.Equal(match.EventId, list![0].EventId);
    }

    [Fact]
    public async Task Filter_ByDistanceRange_ReturnsOnlyEventsInRange()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var tag = Unique("Distance");
        var shortRun = await CreateAsync(organiser, $"{tag} Short", "Running", 5);
        var marathon = await CreateAsync(organiser, $"{tag} Marathon", "Running", 42);

        var list = await _factory.CreateClient()
            .GetFromJsonAsync<List<EventDto>>($"/api/events?search={tag}&minDistanceKm=20&maxDistanceKm=50");

        Assert.Contains(list!, e => e.EventId == marathon.EventId);
        Assert.DoesNotContain(list!, e => e.EventId == shortRun.EventId);
    }

    [Fact]
    public async Task MyEvents_ReturnsOnlyOwnEvents_WithCounts()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var other = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var mine = await TestData.CreateEventAsync(organiser, "My Own Event");
        var theirs = await TestData.CreateEventAsync(other, "Someone Elses Event");
        var category = await TestData.CreateCategoryAsync(organiser, mine.EventId);
        await TestData.EnrolAsync(participant, category.CategoryId);

        var list = await organiser.GetFromJsonAsync<List<OrganiserEventDto>>("/api/events/mine");

        var ownEvent = Assert.Single(list!, e => e.EventId == mine.EventId);
        Assert.Equal(1, ownEvent.CategoryCount);
        Assert.Equal(1, ownEvent.EnrolmentCount);
        Assert.DoesNotContain(list!, e => e.EventId == theirs.EventId);
    }

    [Fact]
    public async Task MyEvents_ForParticipant_ReturnsForbidden()
    {
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var response = await participant.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MyEvents_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_CanChangeTypeAndDistance()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var created = await TestData.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", new UpdateEventRequest
        {
            EventName = "Now A Ride",
            EventDate = DateTime.Now.AddDays(60),
            Location = "Cape Town",
            EventType = "Cycling",
            DistanceKm = 109
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDetailDto>();
        Assert.Equal("Cycling", updated!.EventType);
        Assert.Equal(109m, updated.DistanceKm);
    }

    [Fact]
    public async Task UpdateEvent_WithInvalidType_ReturnsBadRequest()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var created = await TestData.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", new UpdateEventRequest
        {
            EventName = "Bad Type",
            EventDate = DateTime.Now.AddDays(60),
            Location = "Anywhere",
            EventType = "Swimming"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Results_IncludeTotalFinishers()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var first = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var second = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var firstEnrolment = await TestData.EnrolAsync(first, category.CategoryId);
        var secondEnrolment = await TestData.EnrolAsync(second, category.CategoryId);

        await organiser.PostAsJsonAsync($"/api/enrolments/{firstEnrolment.EnrolmentId}/result",
            new CaptureResultRequest { FinishTimeSeconds = 1500, FinishPosition = 1, CategoryPosition = 1 });
        await organiser.PostAsJsonAsync($"/api/enrolments/{secondEnrolment.EnrolmentId}/result",
            new CaptureResultRequest { FinishTimeSeconds = 1700, FinishPosition = 2, CategoryPosition = 2 });

        var history = await first.GetFromJsonAsync<List<ResultDto>>("/api/users/me/history");

        Assert.Equal(2, history![0].TotalFinishers);
        Assert.Equal(1, history[0].FinishPosition);
    }
}
