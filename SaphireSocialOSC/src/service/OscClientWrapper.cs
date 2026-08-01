using BuildSoft.OscCore;
using SaphireSocialOSC.config;
using Serilog;

namespace SaphireSocialOSC.service;

public class OscClientWrapper
{
    private readonly OscClient client;

    public OscClientWrapper(OscClientConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Host)) throw new Exception("Rest Host is required");
        client = new OscClient(config.Host, config.Port);
        Log.Information("Sending OSC messages to {host}:{port}", config.Host, config.Port);
    }

    public void SetParameter(string name, bool value)
    {
        client.Send($"/avatar/parameters/{name}", value);
    }

    public void SetParameter(string name, int value)
    {
        client.Send($"/avatar/parameters/{name}", value);
    }

    public void SetParameter(string name, float value)
    {
        client.Send($"/avatar/parameters/{name}", value);
    }
}