using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>User registration, login and profile.</summary>
public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory) => _factory = factory;

    private static RegisterRequest NewRegistration(string role, string? email = null, string? code = null) => new()
    {
        FullName = "Test User",
        Email = email ?? TestData.UniqueEmail("user"),
        Password = TestData.Password,
        Role = role,
        OrganiserCode = code
    };

    [Fact]
    public async Task Register_Participant_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration(Roles.Participant));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(Roles.Participant, user!.Role);
    }

    [Fact]
    public async Task Register_Organiser_WithValidCode_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            NewRegistration(Roles.Organiser, code: TestData.OrganiserCode));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithoutCode_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration(Roles.Organiser));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongCode_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            NewRegistration(Roles.Organiser, code: "NOT-THE-CODE"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewRegistration("Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration(Roles.Participant);
        request.Password = "short";

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = TestData.UniqueEmail("duplicate");

        var first = await client.PostAsJsonAsync("/api/auth/register", NewRegistration(Roles.Participant, email));
        var second = await client.PostAsJsonAsync("/api/auth/register", NewRegistration(Roles.Participant, email));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotThePlainText()
    {
        var client = _factory.CreateClient();
        var email = TestData.UniqueEmail("hash");

        await client.PostAsJsonAsync("/api/auth/register", NewRegistration(Roles.Participant, email));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = await db.Users.SingleAsync(u => u.Email == email);

        Assert.NotEqual(TestData.Password, stored.PasswordHash);
        Assert.StartsWith("$2", stored.PasswordHash); // BCrypt hash prefix
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndRole()
    {
        var client = _factory.CreateClient();
        var email = TestData.UniqueEmail("login");
        await client.PostAsJsonAsync("/api/auth/register", NewRegistration(Roles.Participant, email));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = TestData.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal(Roles.Participant, body.User.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = TestData.UniqueEmail("wrongpw");
        await client.PostAsJsonAsync("/api/auth/register", NewRegistration(Roles.Participant, email));

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = "Wrong-Password-1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = TestData.UniqueEmail("nobody"), Password = TestData.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithToken_ReturnsOwnProfile()
    {
        var client = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(Roles.Participant, profile!.Role);
    }

    [Fact]
    public async Task UpdateProfile_ChangesNameAndPhone()
    {
        var client = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var response = await client.PutAsJsonAsync("/api/users/me",
            new UpdateProfileRequest { FullName = "Updated Name", PhoneNumber = "0821234567" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await (await client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Updated Name", profile!.FullName);
        Assert.Equal("0821234567", profile.PhoneNumber);
    }

    [Fact]
    public async Task History_ForParticipantWithNoResults_ReturnsNotFound()
    {
        var client = await TestData.LoggedInClientAsync(_factory, Roles.Participant);

        var response = await client.GetAsync("/api/users/me/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
