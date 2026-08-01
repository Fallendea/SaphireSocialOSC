using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SaphireSocialOSC.config;
using SaphireSocialOSC.model;
using SaphireSocialOSC.service;
using Serilog;

namespace SaphireSocialOSC;

internal class Program
{
    private const string DefaultConfigPath = "appsettings.json";
    private const string ConfigPathPrefix = "--config";

    public static async Task Main(string[] args)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetName()
            .Version;
        Console.Out.WriteLine($"Starting Saphire Social OSC in version {version}");

        var configuration = readConfig(args);
        configureLogger(configuration);


        var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        try
        {
            var oscClient = new OscClientWrapper(
                configuration
                    .GetSection(nameof(OscClientConfig))
                    .Get<OscClientConfig>()!
            );

            var eventProcessor = new EventProcessor(
                oscClient,
                configuration
                    .GetSection(nameof(EventMappingConfig))
                    .Get<EventMappingConfig>()!
            );

            var restClient = new RestClient(
                configuration
                    .GetSection(nameof(RestConfig))
                    .Get<RestConfig>()!
            );

            var intervalInSeconds = configuration.GetSection("IntervalInSeconds").Get<int>();
            var interval = TimeSpan.FromSeconds(Math.Max(10, intervalInSeconds));

            await run(restClient, eventProcessor, interval, cts);
        }
        catch (OperationCanceledException)
        {
            Log.Information("Stopping...");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Unhandled exception");
        }
        finally
        {
            Log.CloseAndFlush();
            if (!Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }
    }

    private static async Task run(RestClient restClient, EventProcessor eventProcessor, TimeSpan interval, CancellationTokenSource cts)
    {
        await LogCharacters(restClient);

        Log.Information("Getting Events from {url} every {IntervalInSeconds}s", restClient.PathEvents, interval.Seconds);
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var eventResponse = await restClient.AsyncEventRequest();

                // var groupedEvents = eventResponse?.Events
                //     .GroupBy(e => e.Type)
                //     .ToDictionary(g => g.Key, g => g.ToList());

                var groupedEvents =
                    eventResponse?.Events
                        .GroupBy(e => e.CharacterUsername)
                        .ToDictionary(
                            g => g.Key,
                            g => g.GroupBy(e => e.Type)
                                .ToDictionary(
                                    x => x.Key,
                                    x => x.ToList()));

                if (groupedEvents?.Count > 0)
                {
                    Log.Information("Received {count} events:", eventResponse!.Events.Count);
                    eventProcessor.process(groupedEvents);
                }
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
                Log.Error("Request timed out ({TimeoutInSeconds}s).", restClient.TimeoutInSeconds);
            }
            catch (NullReferenceException ex)
            {
                Log.Error(ex, "NPE Error");
            }

            await Task.Delay(interval, cts.Token);
        }
    }

    private static async Task LogCharacters(RestClient restClient, int maxRetries = 3)
    {
        for (var attempt = 1; attempt <= maxRetries; attempt++)
            try
            {
                var response = await restClient.AsyncMeRequest();

                foreach (var character in response!.Characters)
                    Log.Information("Used token is for character {DisplayName} ( @{Username} )", character.DisplayName, character.Username);

                Log.Information(new string('-', 40));

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                Log.Warning(ex, "Failed to get characters (attempt {Attempt}/{MaxRetries}). Retrying...", attempt, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get characters after {MaxRetries} attempts", maxRetries);
                throw;
            }
    }

    private static IConfigurationRoot readConfig(string[] args)
    {
        var configPath = DefaultConfigPath;
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] != ConfigPathPrefix) continue;
            configPath = args[i + 1];
            Console.Out.WriteLine($"Config path overridden with {configPath}");
            break;
        }

        if (!Path.IsPathRooted(configPath)) configPath = Path.Combine(AppContext.BaseDirectory, configPath);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(configPath, false, true)
            .Build();
        return configuration;
    }

    private static void configureLogger(IConfigurationRoot configuration)
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 2
            )
            .CreateLogger();

        Log.Information("Logging initialized. Log files will be written to: {LogPath}", Path.GetFullPath(logDirectory));
    }
}