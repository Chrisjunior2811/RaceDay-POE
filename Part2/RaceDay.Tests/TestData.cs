using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

/// <summary>Helpers that register/login users and create events, categories and enrolments for the tests.</summary>
public static class TestData
{
    public const string Password = "Passw0rd!123";
    public const string OrganiserCode = "RACEDAY-ORGANISER-2026"; // same value as appsettings.json

    public static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.com";

    /// <summary>Registers a new user with the given role, logs in, and returns a client carrying the JWT.</summary>
    public static async Task<HttpClient> LoggedInClientAsync(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail(role.ToLowerInvariant());

        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"Test {role}",
            Email = email,
            Password = Password,
            Role = role,
            OrganiserCode = role == Roles.Organiser ? OrganiserCode : null
        });
        register.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });
        login.EnsureSuccessStatusCode();

        var body = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        return client;
    }

    public static CreateEventRequest NewEventRequest(string name = "Test Park Run") => new()
    {
        EventName = name,
        EventType = "Running",
        EventDate = DateTime.Now.AddDays(30),
        Location = "Johannesburg, Gauteng",
        Description = "A test event.",
        Route = new RouteRequest { DistanceKm = 5, StartPoint = "Delta Park", EndPoint = "Delta Park" }
    };

    public static async Task<EventDetailDto> CreateEventAsync(HttpClient organiser, string name = "Test Park Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEventRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDetailDto>())!;
    }

    public static async Task<CategoryDto> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryRequest
        {
            CategoryName = "5km Fun Run",
            DistanceKm = 5,
            EntryFee = 50,
            MaxParticipants = maxParticipants
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }
}
