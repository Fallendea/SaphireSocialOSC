using SaphireSocialOSC.config;
using SaphireSocialOSC.model;
using Serilog;

namespace SaphireSocialOSC.service;

public class EventProcessor
{
    private readonly Dictionary<string, Dictionary<string, int>> countCache = new();
    private readonly EventMappingConfig config;
    private readonly OscClientWrapper oscClient;

    public EventProcessor(OscClientWrapper oscClient, EventMappingConfig config)
    {
        this.oscClient = oscClient;
        this.config = config;
        config.validate();
    }

    public void process(Dictionary<string, Dictionary<string, List<Event>>> characterEvents)
    {
        foreach (var (characterName, groupedEvents) in characterEvents)
        {
            foreach (var (eventType, eventList) in groupedEvents)
            {
                Log.Debug("@{characterName}: Got {currentCount} new events of type '{eventType}'.", characterName, eventList.Count, eventType);
                if (config.TryGet(characterName, eventType, out var oscParameterConfig) && (oscParameterConfig?.Enabled ?? true))
                {
                    var count = UpdateCache(characterName, eventType, eventList.Count);
                    Log.Debug("@{characterName}: Parameter type for '{eventType}' is {parameterType}. Full count is {count}",
                        characterName, eventType, oscParameterConfig!.Type, count);
                    var oscParameter = oscParameterConfig.Parameter!;
                    switch (oscParameterConfig.Type)
                    {
                        case OscParameterType.Bool:
                            oscClient.SetParameter(oscParameter, true);
                            break;
                        case OscParameterType.Int:
                            oscClient.SetParameter(oscParameter, count);
                            break;
                        case OscParameterType.Float:
                            var percent = (count - oscParameterConfig.Min!.Value) / (oscParameterConfig.Max!.Value - oscParameterConfig.Min!.Value);
                            oscClient.SetParameter(oscParameter, Math.Clamp(percent, 0f, 1f));
                            break;
                    }
                }
                else if (oscParameterConfig == null)
                {
                    Log.Debug("@{characterName}: Could not find a osc parameter config for type '{eventType}'", characterName, eventType);
                }
                else
                {
                    Log.Debug("@{characterName}: Type '{eventType}' is disabled", characterName, eventType);
                }
            }
        }
    }

    private int UpdateCache(string characterName, string eventType, int count)
    {
        if (!countCache.TryGetValue(characterName, out var characterCache))
        {
            characterCache = new Dictionary<string, int>();
            countCache[characterName] = characterCache;
        }

        characterCache[eventType] = characterCache.GetValueOrDefault(eventType) + count;

        return characterCache[eventType];
    }
}