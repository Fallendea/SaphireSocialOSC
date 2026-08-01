using System.Reflection;
using Microsoft.Extensions.Configuration;
using SaphireSocialOSC.config;
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
            await startPolling(configuration, cts);
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
        }
    }

    private static async Task startPolling(IConfigurationRoot configuration, CancellationTokenSource cts)
    {
        var oscClient = new OscClientWrapper(
            configuration
                .GetSection(nameof(OscClientConfig))
                .Get<OscClientConfig>()!
        );

        var restClient = new RestClient(
            configuration
                .GetSection(nameof(RestConfig))
                .Get<RestConfig>()!
        );
        await restClient.RunAsync(cts.Token);
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

        if (!Path.IsPathRooted(configPath))
        {
            configPath = Path.Combine(AppContext.BaseDirectory, configPath);
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
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