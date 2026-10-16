using System.Net;
using System.Net.Http.Json;
using RaceDay.Api;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>Results: organisers capture them, participants can view their own.</summary>
public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory) => _factory = factory;

    private static CaptureResultRequest FinishedResult() => new()
    {
        FinishTimeSeconds = 1523,
        FinishPosition = 1,
        CategoryPosition = 1,
        Status = "Finished"
    };

    [Fact]
    public async Task Organiser_CanCaptureResult_AndItIsPubliclyVisible()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", FinishedResult());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal("00:25:23", result!.FinishTime);

        var publicList = await _factory.CreateClient().GetFromJsonAsync<List<ResultDto>>($"/api/events/{evt.EventId}/results");
        Assert.Contains(publicList!, r => r.ResultId == result.ResultId);

        var single = await _factory.CreateClient().GetAsync($"/api/results/{result.ResultId}");
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task Participant_CanSeeOwnResultInHistory()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", FinishedResult());

        var response = await participant.GetAsync("/api/users/me/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var history = await response.Content.ReadFromJsonAsync<List<ResultDto>>();
        Assert.Contains(history!, r => r.EnrolmentId == enrolment.EnrolmentId);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", FinishedResult());

        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", FinishedResult());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTime_ReturnsBadRequest()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result",
            new CaptureResultRequest { Status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForMissingEnrolment_ReturnsNotFound()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);

        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/result", FinishedResult());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent()
    {
        var owner = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var other = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(owner);
        var category = await TestData.CreateCategoryAsync(owner, evt.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/result", FinishedResult());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetResult_Missing_ReturnsNotFound()
    {
        var response = await _factory.CreateClient().GetAsync("/api/results/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
