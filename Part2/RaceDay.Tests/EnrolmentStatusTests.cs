using System.Net;
using System.Net.Http.Json;
using RaceDay.Api;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>Organisers confirm enrolments (Paid shows as "Confirmed" in the web app).</summary>
public class EnrolmentStatusTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentStatusTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(HttpClient Organiser, HttpClient Participant, EnrolmentDto Enrolment)> EnrolledAsync()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);
        var category = await TestData.CreateCategoryAsync(organiser, evt.EventId);
        var enrolment = await TestData.EnrolAsync(participant, category.CategoryId);
        return (organiser, participant, enrolment);
    }

    [Fact]
    public async Task NewEnrolment_StartsAsPending()
    {
        var (_, _, enrolment) = await EnrolledAsync();

        Assert.Equal("Pending", enrolment.PaymentStatus);
        Assert.False(enrolment.HasResult);
    }

    [Fact]
    public async Task Organiser_CanConfirmAnEnrolment()
    {
        var (organiser, participant, enrolment) = await EnrolledAsync();

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest { PaymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal("Paid", updated!.PaymentStatus);
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/me");
        Assert.Contains(mine!, e => e.EnrolmentId == enrolment.EnrolmentId && e.PaymentStatus == "Paid");
    }

    [Fact]
    public async Task InvalidStatus_ReturnsBadRequest()
    {
        var (organiser, _, enrolment) = await EnrolledAsync();

        var response = await organiser.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest { PaymentStatus = "Whatever" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotChangeStatus()
    {
        var (_, participant, enrolment) = await EnrolledAsync();

        var response = await participant.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest { PaymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotChangeStatus()
    {
        var (_, _, enrolment) = await EnrolledAsync();
        var other = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);

        var response = await other.PutAsJsonAsync($"/api/enrolments/{enrolment.EnrolmentId}/status",
            new UpdateEnrolmentStatusRequest { PaymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnknownEnrolment_ReturnsNotFound()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);

        var response = await organiser.PutAsJsonAsync("/api/enrolments/999999/status",
            new UpdateEnrolmentStatusRequest { PaymentStatus = "Paid" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
