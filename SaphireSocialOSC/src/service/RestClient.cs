using System.Net.Http.Headers;
using System.Text.Json;
using SaphireSocialOSC.config;
using SaphireSocialOSC.model;
using Serilog;

namespace SaphireSocialOSC;

public class RestClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient client;
    private readonly string Token;
    private readonly int IntervalInSeconds;
    private readonly int TimeoutInSeconds;

    private readonly string PathMe;
    private readonly string PathEvents;

    private long? currentCursor = null;

    public RestClient(RestConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Host)) throw new Exception("Rest Host is required");
        if (string.IsNullOrWhiteSpace(config.Token)) throw new Exception("Rest Token is required");

        Token = config.Token;
        IntervalInSeconds = Math.Max(10, config.IntervalInSeconds);
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

    private async Task<MeResponseBody?> AsyncMeRequest()
    {
        using var response = await client.GetAsync($"{PathMe}");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<MeResponseBody>(json, JsonOptions);
    }

    private async Task<EventResponseBody?> AsyncEventRequest()
    {
        using var response = currentCursor == null
            ? await client.GetAsync($"{PathEvents}")
            : await client.GetAsync($"{PathEvents}?after={currentCursor}");

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<EventResponseBody>(json, JsonOptions);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Log.Information("Getting Events from {url} every {IntervalInSeconds}s", PathEvents, IntervalInSeconds);

        await LogTokenInformation();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await AsyncEventRequest();
                currentCursor = response!.Cursor;

                var groupedEvents = response.Events
                    .GroupBy(e => e.Type)
                    .ToDictionary(g => g.Key, g => g.ToList());

                LogEventStatus(groupedEvents, response);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Json Error");
            }
            catch (HttpRequestException ex)
            {
                Log.Error(ex, "HTTP Error");
            }
            catch (TaskCanceledException)
            {
                Log.Error("Request timed out ({TimeoutInSeconds}s).", TimeoutInSeconds);
            }

            await Task.Delay(TimeSpan.FromSeconds(IntervalInSeconds), cancellationToken);
        }
    }

    private void LogEventStatus(Dictionary<EventType, List<Event>> groupedEvents, EventResponseBody eventResponseBody)
    {
        if (groupedEvents.Count == 0) return;

        Log.Information("Received {count} events:", eventResponseBody.Events.Count);

        if (!Log.IsEnabled(Serilog.Events.LogEventLevel.Debug)) return;
        foreach (var (eventType, eventList) in groupedEvents)
        {
            Log.Debug("  {eventType}: {eventCount}", eventType, eventList.Count);
        }

        Log.Debug(new string('-', 40));
    }

    private async Task LogTokenInformation()
    {
        try
        {
            var response = await AsyncMeRequest();
            foreach (var character in response!.Characters)
            {
                Log.Information("Used token is for character {displayName} ( @{username} )", character.DisplayName, character.Username);
            }

            Log.Information(new string('-', 40));
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Json Error");
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "HTTP Error");
        }
        catch (TaskCanceledException)
        {
            Log.Error("Request timed out ({TimeoutInSeconds}s).", TimeoutInSeconds);
        }
    }
}