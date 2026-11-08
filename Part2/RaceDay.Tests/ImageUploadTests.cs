using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>Event banner and profile picture uploads (stored in blob storage through the API).</summary>
public class ImageUploadTests : IClassFixture<RaceDayFactory>
{
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };

    private readonly RaceDayFactory _factory;

    public ImageUploadTests(RaceDayFactory factory) => _factory = factory;

    private static MultipartFormDataContent Upload(byte[] bytes, string fileName = "image.png")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    [Fact]
    public async Task Organiser_CanUploadBanner_AndAnyoneCanDownloadIt()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(organiser);

        var upload = await organiser.PostAsync($"/api/events/{evt.EventId}/banner", Upload(PngBytes));
        var download = await _factory.CreateClient().GetAsync($"/api/events/{evt.EventId}/banner");
        var detail = await _factory.CreateClient().GetFromJsonAsync<EventDetailDto>($"/api/events/{evt.EventId}");

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes.Length, (await download.Content.ReadAsByteArrayAsync()).Length);
        Assert.True(detail!.HasBanner);
    }

    [Fact]
    public async Task ReplacingBanner_RemovesTheOldImageFromStorage()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(organiser);

        await organiser.PostAsync($"/api/events/{evt.EventId}/banner", Upload(PngBytes));
        var afterFirst = _factory.Blobs.Count;
        await organiser.PostAsync($"/api/events/{evt.EventId}/banner", Upload(PngBytes));

        Assert.Equal(afterFirst, _factory.Blobs.Count);
    }

    [Fact]
    public async Task GetBanner_WhenEventHasNone_ReturnsNotFound()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(organiser);

        var response = await _factory.CreateClient().GetAsync($"/api/events/{evt.EventId}/banner");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsync("/api/events/1/banner", Upload(PngBytes));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUploadBanner()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);
        var evt = await TestData.CreateEventAsync(organiser);

        var response = await participant.PostAsync($"/api/events/{evt.EventId}/banner", Upload(PngBytes));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUploadBannerForAnotherOrganisersEvent()
    {
        var owner = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var other = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(owner);

        var response = await other.PostAsync($"/api/events/{evt.EventId}/banner", Upload(PngBytes));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OfANonImage_ReturnsBadRequest()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(organiser);
        var notAnImage = System.Text.Encoding.UTF8.GetBytes("this is just text, not a picture");

        var response = await organiser.PostAsync($"/api/events/{evt.EventId}/banner", Upload(notAnImage, "fake.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_OfAnOversizedImage_ReturnsBadRequest()
    {
        var organiser = await TestData.LoggedInClientAsync(_factory, Roles.Organiser);
        var evt = await TestData.CreateEventAsync(organiser);
        var tooBig = new byte[(5 * 1024 * 1024) + 1];
        PngBytes.CopyTo(tooBig, 0);

        var response = await organiser.PostAsync($"/api/events/{evt.EventId}/banner", Upload(tooBig));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task User_CanUploadAndDownloadOwnProfilePicture()
    {
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var upload = await participant.PostAsync("/api/users/me/picture", Upload(PngBytes));
        var download = await participant.GetAsync("/api/users/me/picture");
        var profile = await participant.GetFromJsonAsync<UserDto>("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.True(profile!.HasProfilePicture);
    }

    [Fact]
    public async Task ProfilePicture_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/users/me/picture");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProfilePicture_WhenNoneUploaded_ReturnsNotFound()
    {
        var participant = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var response = await participant.GetAsync("/api/users/me/picture");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
