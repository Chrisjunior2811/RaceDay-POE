using System.Net;
using System.Net.Http.Json;
using RaceDay.Api;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>Proves unauthenticated requests get 401 and the wrong role gets 403.</summary>
public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory) => _factory = factory;

    // ---------- Unauthenticated (no token) -> 401 ----------

    [Fact]
    public async Task CreateEvent_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/events", TestData.NewEventRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsync("/api/categories/1/enrol", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MyEnrolments_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/enrolments/1/result",
            new CaptureResultRequest { FinishTimeSeconds = 1000 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Participant calling Organiser functionality -> 403 ----------

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var response = await participant.PostAsJsonAsync("/api/events", TestData.NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);

        var update = await participant.PutAsJsonAsync($"/api/events/{evt.EventId}", new UpdateEventRequest
        {
            EventName = "Hacked",
            EventDate = DateTime.Now.AddDays(10),
            Location = "Nowhere"
        });
        var delete = await participant.DeleteAsync($"/api/events/{evt.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{evt.EventId}/categories",
            new CategoryRequest { CategoryName = "Sneaky", DistanceKm = 5, EntryFee = 0, MaxParticipants = 10 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/events/{evt.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult()
    {
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var response = await participant.PostAsJsonAsync("/api/enrolments/1/result",
            new CaptureResultRequest { FinishTimeSeconds = 1000 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Organiser calling Participant functionality -> 403 ----------

    [Fact]
    public async Task Organiser_CannotEnrol()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);

        var response = await organiser.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantHistoryOrEnrolments()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);

        var history = await organiser.GetAsync("/api/users/me/history");
        var mine = await organiser.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Forbidden, history.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, mine.StatusCode);
    }

    // ---------- Organiser acting on another organiser's event -> 403 ----------

    [Fact]
    public async Task Organiser_CannotChangeAnotherOrganisersEvent()
    {
        var owner = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var other = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(owner);

        var update = await other.PutAsJsonAsync($"/api/events/{evt.EventId}", new UpdateEventRequest
        {
            EventName = "Stolen",
            EventDate = DateTime.Now.AddDays(10),
            Location = "Elsewhere"
        });
        var delete = await other.DeleteAsync($"/api/events/{evt.EventId}");
        var addCategory = await other.PostAsJsonAsync($"/api/events/{evt.EventId}/categories",
            new CategoryRequest { CategoryName = "Nope", DistanceKm = 5, EntryFee = 0, MaxParticipants = 10 });
        var enrolments = await other.GetAsync($"/api/events/{evt.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, addCategory.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, enrolments.StatusCode);
    }
}
