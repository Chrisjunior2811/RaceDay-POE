using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Infrastructure;

/// <summary>Marker type for calls that return no body.</summary>
public sealed class NoContent { }

public record ImageData(byte[] Bytes, string ContentType);

public class ApiResponse
{
    public bool Success { get; init; }
    public HttpStatusCode Status { get; init; }
    public string? Error { get; init; }
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }
}

/// <summary>
/// The only way the MVC app gets or changes data: it calls the Part 2 API over HTTP.
/// The logged-in user's token is read from the session and sent with every request.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    // ---------- Authentication and profile ----------

    public Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request) =>
        PostJsonAsync<LoginResponse>("api/auth/login", request);

    public Task<ApiResponse<UserDto>> RegisterAsync(RegisterRequest request) =>
        PostJsonAsync<UserDto>("api/auth/register", request);

    public Task<ApiResponse<UserDto>> GetProfileAsync() => GetAsync<UserDto>("api/users/me");

    public Task<ApiResponse<UserDto>> UpdateProfileAsync(UpdateProfileRequest request) =>
        PutJsonAsync<UserDto>("api/users/me", request);

    public Task<ApiResponse<NoContent>> UploadProfilePictureAsync(IFormFile file) =>
        UploadAsync("api/users/me/picture", file);

    public Task<ImageData?> GetProfilePictureAsync() => GetImageAsync("api/users/me/picture");

    public Task<ApiResponse<List<ResultDto>>> GetMyHistoryAsync() =>
        GetAsync<List<ResultDto>>("api/users/me/history");

    // ---------- Events ----------

    public Task<ApiResponse<List<EventDto>>> GetUpcomingEventsAsync(
        string? type, string? search, decimal? minDistanceKm, decimal? maxDistanceKm)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(type))
        {
            query.Add("type=" + Uri.EscapeDataString(type));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        }

        if (minDistanceKm.HasValue)
        {
            query.Add("minDistanceKm=" + minDistanceKm.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (maxDistanceKm.HasValue)
        {
            query.Add("maxDistanceKm=" + maxDistanceKm.Value.ToString(CultureInfo.InvariantCulture));
        }

        var url = query.Count == 0 ? "api/events" : "api/events?" + string.Join("&", query);
        return GetAsync<List<EventDto>>(url);
    }

    public Task<ApiResponse<EventDetailDto>> GetEventAsync(int id) =>
        GetAsync<EventDetailDto>($"api/events/{id}");

    public Task<ApiResponse<List<OrganiserEventDto>>> GetMyEventsAsync() =>
        GetAsync<List<OrganiserEventDto>>("api/events/mine");

    public Task<ApiResponse<EventDetailDto>> CreateEventAsync(CreateEventRequest request) =>
        PostJsonAsync<EventDetailDto>("api/events", request);

    public Task<ApiResponse<EventDetailDto>> UpdateEventAsync(int id, UpdateEventRequest request) =>
        PutJsonAsync<EventDetailDto>($"api/events/{id}", request);

    public Task<ApiResponse<NoContent>> DeleteEventAsync(int id) => DeleteAsync($"api/events/{id}");

    public Task<ApiResponse<NoContent>> UploadEventBannerAsync(int id, IFormFile file) =>
        UploadAsync($"api/events/{id}/banner", file);

    public Task<ImageData?> GetEventBannerAsync(int id) => GetImageAsync($"api/events/{id}/banner");

    // ---------- Categories ----------

    public Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync(int eventId) =>
        GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");

    public Task<ApiResponse<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest request) =>
        PostJsonAsync<CategoryDto>($"api/events/{eventId}/categories", request);

    public Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(int id, CategoryRequest request) =>
        PutJsonAsync<CategoryDto>($"api/categories/{id}", request);

    public Task<ApiResponse<NoContent>> DeleteCategoryAsync(int id) => DeleteAsync($"api/categories/{id}");

    // ---------- Enrolments ----------

    public Task<ApiResponse<EnrolmentDto>> EnrolAsync(int categoryId) =>
        PostEmptyAsync<EnrolmentDto>($"api/categories/{categoryId}/enrol");

    public Task<ApiResponse<List<EnrolmentDto>>> GetMyEnrolmentsAsync() =>
        GetAsync<List<EnrolmentDto>>("api/enrolments/me");

    public Task<ApiResponse<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId) =>
        GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");

    public Task<ApiResponse<NoContent>> CancelEnrolmentAsync(int id) => DeleteAsync($"api/enrolments/{id}");

    public Task<ApiResponse<EnrolmentDto>> SetEnrolmentStatusAsync(int id, string paymentStatus) =>
        PutJsonAsync<EnrolmentDto>($"api/enrolments/{id}/status",
            new UpdateEnrolmentStatusRequest { PaymentStatus = paymentStatus });

    // ---------- Results ----------

    public Task<ApiResponse<ResultDto>> CaptureResultAsync(int enrolmentId, CaptureResultRequest request) =>
        PostJsonAsync<ResultDto>($"api/enrolments/{enrolmentId}/result", request);

    public Task<ApiResponse<List<ResultDto>>> GetEventResultsAsync(int eventId) =>
        GetAsync<List<ResultDto>>($"api/events/{eventId}/results");

    // ---------- Images ----------

    private async Task<ImageData?> GetImageAsync(string url)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return new ImageData(bytes, contentType);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    // ---------- Plumbing ----------

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        var token = _accessor.HttpContext?.Session.GetString(SessionKeys.Token);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private Task<ApiResponse<T>> GetAsync<T>(string url) =>
        SendAsync<T>(CreateRequest(HttpMethod.Get, url));

    private Task<ApiResponse<T>> PostJsonAsync<T>(string url, object body) =>
        SendAsync<T>(CreateRequest(HttpMethod.Post, url, JsonContent.Create(body, body.GetType())));

    private Task<ApiResponse<T>> PutJsonAsync<T>(string url, object body) =>
        SendAsync<T>(CreateRequest(HttpMethod.Put, url, JsonContent.Create(body, body.GetType())));

    private Task<ApiResponse<T>> PostEmptyAsync<T>(string url) =>
        SendAsync<T>(CreateRequest(HttpMethod.Post, url));

    private Task<ApiResponse<NoContent>> DeleteAsync(string url) =>
        SendAsync<NoContent>(CreateRequest(HttpMethod.Delete, url));

    private Task<ApiResponse<NoContent>> UploadAsync(string url, IFormFile file)
    {
        var fileContent = new StreamContent(file.OpenReadStream());
        var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        var form = new MultipartFormDataContent { { fileContent, "file", file.FileName } };
        return SendAsync<NoContent>(CreateRequest(HttpMethod.Post, url, form));
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request)
    {
        using (request)
        {
            try
            {
                using var response = await _http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    T? data = default;
                    if (typeof(T) != typeof(NoContent) && response.StatusCode != HttpStatusCode.NoContent)
                    {
                        data = await response.Content.ReadFromJsonAsync<T>();
                    }

                    return new ApiResponse<T> { Success = true, Status = response.StatusCode, Data = data };
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // The API no longer accepts our token (expired or invalid): end the session.
                    _accessor.HttpContext?.Session.Clear();
                }

                return new ApiResponse<T>
                {
                    Success = false,
                    Status = response.StatusCode,
                    Error = await ReadErrorAsync(response)
                };
            }
            catch (HttpRequestException)
            {
                return Failed<T>(HttpStatusCode.ServiceUnavailable,
                    "The RaceDay service is not reachable right now. Please try again shortly.");
            }
            catch (TaskCanceledException)
            {
                return Failed<T>(HttpStatusCode.GatewayTimeout,
                    "The RaceDay service took too long to respond. Please try again.");
            }
            catch (JsonException)
            {
                return Failed<T>(HttpStatusCode.BadGateway,
                    "The RaceDay service sent a response we could not understand. Please try again.");
            }
        }
    }

    private static ApiResponse<T> Failed<T>(HttpStatusCode status, string message) =>
        new() { Success = false, Status = status, Error = message };

    /// <summary>Turns an API error response into one short, friendly sentence for the user.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(body))
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = errors.EnumerateObject()
                            .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                            .SelectMany(p => p.Value.EnumerateArray())
                            .Where(v => v.ValueKind == JsonValueKind.String)
                            .Select(v => v.GetString())
                            .Where(s => !string.IsNullOrWhiteSpace(s));

                        var joined = string.Join(" ", messages);
                        if (joined.Length > 0)
                        {
                            return joined;
                        }
                    }

                    if (root.TryGetProperty("detail", out var detail)
                        && detail.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(detail.GetString()))
                    {
                        return detail.GetString()!;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // fall through to the generic message below
        }

        return response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired or you are not logged in. Please log in again.",
            HttpStatusCode.Forbidden => "You do not have permission to do that.",
            HttpStatusCode.NotFound => "That item could not be found.",
            HttpStatusCode.ServiceUnavailable => "That service is not available right now. Please try again later.",
            _ => "Something went wrong. Please try again."
        };
    }
}
