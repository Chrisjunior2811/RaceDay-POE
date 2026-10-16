using System.Net;
using System.Net.Http.Json;
using RaceDay.Api;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>Event enrolments: participants enter a category and the link is recorded.</summary>
public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsCorrectlyRecorded()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);

        var response = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal(category.CategoryId, enrolment!.CategoryId);
        Assert.Equal(evt.EventId, enrolment.EventId);
        Assert.StartsWith("BIB", enrolment.BibNumber);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Organiser_CanSeeEnrolmentsForTheirEvent()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.GetAsync($"/api/events/{evt.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>();
        Assert.Contains(list!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        await TestData.EnrolAsync(participant, category.CategoryId);

        var second = await participant.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_InFullCategory_ReturnsConflict()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var first = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var second = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId, maxParticipants: 1);
        await TestData.EnrolAsync(first, category.CategoryId);

        var response = await second.PostAsync($"/api/categories/{category.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InMissingCategory_ReturnsNotFound()
    {
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var response = await participant.PostAsync("/api/categories/999999/enrol", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.DoesNotContain(mine!, e => e.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var owner = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var other = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var enrolment = await TestData.EnrolAsync(owner, category.CategoryId);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
