using System.Net;
using System.Net.Http.Json;
using RaceDay.Api;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>Event and category management by Organisers.</summary>
public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task ListEvents_Anonymous_ReturnsOk()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_AndItAppearsInTheList()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);

        var created = await TestData.CreateEventAsync(organiser, "Listed Event");

        Assert.Equal("Listed Event", created.EventName);
        Assert.Equal(5m, created.DistanceKm);
        var list = await _factory.CreateClient().GetFromJsonAsync<List<EventDto>>("/api/events");
        Assert.Contains(list!, e => e.EventId == created.EventId);
    }

    [Fact]
    public async Task GetEventById_ReturnsEventWithRoute()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var created = await TestData.CreateEventAsync(organiser);

        var response = await _factory.CreateClient().GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<EventDetailDto>();
        Assert.NotNull(detail!.Route);
    }

    [Fact]
    public async Task GetEventById_Missing_ReturnsNotFound()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidType_ReturnsBadRequest()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var request = TestData.NewEventRequest();
        request.EventType = "Swimming";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithPastDate_ReturnsBadRequest()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var request = TestData.NewEventRequest();
        request.EventDate = DateTime.Now.AddDays(-1);

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var created = await TestData.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", new UpdateEventRequest
        {
            EventName = "Renamed Event",
            EventDate = DateTime.Now.AddDays(45),
            Location = "Cape Town",
            Description = "Updated"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDetailDto>();
        Assert.Equal("Renamed Event", updated!.EventName);
        Assert.Equal("Cape Town", updated.Location);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var created = await TestData.CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{created.EventId}");
        var get = await _factory.CreateClient().GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_ReturnsConflict()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.DeleteAsync($"/api/events/{evt.EventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanAddAndListCategories()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);

        var list = await _factory.CreateClient()
            .GetFromJsonAsync<List<CategoryDto>>($"/api/events/{evt.EventId}/categories");

        Assert.Contains(list!, c => c.CategoryId == category.CategoryId);
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteCategory()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);

        var update = await organiser.PutAsJsonAsync($"/api/categories/{category.CategoryId}", new CategoryRequest
        {
            CategoryName = "10km Challenge",
            DistanceKm = 10,
            EntryFee = 80,
            MaxParticipants = 200
        });
        var delete = await organiser.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_ReturnsConflict()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
