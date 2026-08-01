using System.Net.Http.Headers;
using System.Text.Json;
using SaphireSocialOSC.config;
using SaphireSocialOSC.model;

namespace SaphireSocialOSC.service;

public class RestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient client;
    public readonly string PathEvents;

    private readonly string PathMe;
    public readonly int TimeoutInSeconds;
    private readonly string Token;

    private long? currentCursor;

    public RestClient(RestConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Host)) throw new Exception("Rest Host is required");
        if (string.IsNullOrWhiteSpace(config.Token)) throw new Exception("Rest Token is required");

        Token = config.Token;
        TimeoutInSeconds = config.TimeoutInSeconds;
        client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(TimeoutInSeconds)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

        var baseUrl = config.Host.TrimEnd('/');
        PathMe = $"{baseUrl}/api/me";
        PathEvents = $"{baseUrl}/api/events";
    }

    public async Task<MeResponseBody?> AsyncMeRequest()
    {
        using var response = await client.GetAsync($"{PathMe}");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<MeResponseBody>(json, JsonOptions);
    }

    public async Task<EventResponseBody?> AsyncEventRequest()
    {
        using var response = currentCursor == null
            ? await client.GetAsync($"{PathEvents}")
            : await client.GetAsync($"{PathEvents}?after={currentCursor}");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var responseBody = JsonSerializer.Deserialize<EventResponseBody>(json, JsonOptions);
        currentCursor = responseBody?.Cursor;
        return responseBody;
    }
}