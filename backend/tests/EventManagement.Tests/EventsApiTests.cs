using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EventManagement.Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventManagement.Tests;

/// <summary>
/// End-to-end tests over HTTP: routing, model validation, status codes and problem-details shape.
/// Business rules themselves are covered in <see cref="EventServiceTests"/>.
/// </summary>
public sealed class EventsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _client;

    public EventsApiTests(WebApplicationFactory<Program> factory) =>
        _client = factory.WithWebHostBuilder(b => b.UseSetting("SeedSampleData", "false")).CreateClient();

    private async Task<EventResponse> CreateEventAsync(int capacity = 5)
    {
        var response = await _client.PostAsJsonAsync("/api/events", new
        {
            title = "Integration Test Event",
            description = "Created by a test",
            date = DateTimeOffset.UtcNow.AddDays(3),
            maxCapacity = capacity,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EventResponse>(Json))!;
    }

    [Fact]
    public async Task PostEvent_ReturnsCreatedWithLocation_ThenGetReturnsIt()
    {
        var created = await CreateEventAsync();

        var fetched = await _client.GetFromJsonAsync<EventResponse>($"/api/events/{created.Id}", Json);

        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task PostEvent_WithInvalidPayload_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/events", new { title = "", maxCapacity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Contains("Title", problem!.Errors.Keys);
        Assert.Contains("MaxCapacity", problem.Errors.Keys);
        Assert.Contains("Date", problem.Errors.Keys);
    }

    [Fact]
    public async Task PostEvent_WithPastDate_ReturnsBadRequestWithCode()
    {
        var response = await _client.PostAsJsonAsync("/api/events", new
        {
            title = "Too late",
            date = DateTimeOffset.UtcNow.AddDays(-1),
            maxCapacity = 5,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("event_date_in_past", await ReadErrorCode(response));
    }

    [Fact]
    public async Task GetEvent_Unknown_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("event_not_found", await ReadErrorCode(response));
    }

    [Fact]
    public async Task RegistrationFlow_EnforcesDuplicateAndCapacityRules()
    {
        var evt = await CreateEventAsync(capacity: 1);
        var url = $"/api/events/{evt.Id}/registrations";

        var first = await _client.PostAsJsonAsync(url, new { name = "Ada", email = "ada@example.com" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var registration = await first.Content.ReadFromJsonAsync<RegistrationResponse>(Json);

        var duplicate = await _client.PostAsJsonAsync(url, new { name = "Ada", email = "ADA@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("already_registered", await ReadErrorCode(duplicate));

        var full = await _client.PostAsJsonAsync(url, new { name = "Alan", email = "alan@example.com" });
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Equal("event_full", await ReadErrorCode(full));

        var delete = await _client.DeleteAsync($"{url}/{registration!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var retry = await _client.PostAsJsonAsync(url, new { name = "Alan", email = "alan@example.com" });
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);

        var attendees = await _client.GetFromJsonAsync<List<RegistrationResponse>>(url, Json);
        Assert.Equal("alan@example.com", Assert.Single(attendees!).Email);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsValidationProblem()
    {
        var evt = await CreateEventAsync();

        var response = await _client.PostAsJsonAsync($"/api/events/{evt.Id}/registrations", new { name = "Ada", email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_AcceptsStatusFilter()
    {
        var evt = await CreateEventAsync();

        var upcoming = await _client.GetFromJsonAsync<List<EventResponse>>("/api/events?status=upcoming", Json);
        var past = await _client.GetFromJsonAsync<List<EventResponse>>("/api/events?status=past", Json);

        Assert.Contains(upcoming!, e => e.Id == evt.Id);
        Assert.DoesNotContain(past!, e => e.Id == evt.Id);
    }

    [Fact]
    public async Task DeleteEvent_ReturnsNoContent_ThenNotFound()
    {
        var evt = await CreateEventAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/events/{evt.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/events/{evt.Id}")).StatusCode);
    }

    private static async Task<string?> ReadErrorCode(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
