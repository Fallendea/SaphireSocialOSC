using SaphireSocialOSC.model;

namespace SaphireSocialOSC.config;

public class EventMappingConfig
{
    public EventMapping? Default { get; set; } = new();
    public Dictionary<string, EventMapping?>? CharacterSpecificMapping { get; set; } = new();

    public bool TryGet(string character, string eventType, out OscParameterConfig? config)
    {
        var eventMappingConfig = CharacterSpecificMapping!.GetValueOrDefault($"@{character}", Default);
        config = eventMappingConfig?.GetValueOrDefault(eventType, Default?.GetValueOrDefault(eventType, null));
        return config != null;
    }

    public void validate()
    {
        if (Default is null) throw new ArgumentNullException($"{nameof(Default)} should not be null");
        validateEventMapping(Default!, "'Default'");

        if (CharacterSpecificMapping is null) CharacterSpecificMapping = new();
        var characterNamesToChange = CharacterSpecificMapping.Keys
            .Where(k => !k.StartsWith("@"))
            .ToList();


        CharacterSpecificMapping
            .Where(x => x.Value == null)
            .Select(x => x.Key)
            .ToList()
            .ForEach(key => CharacterSpecificMapping.Remove(key));

        foreach (var (characterName, eventMapping) in CharacterSpecificMapping)
        {
            var identifier = $"Character '{characterName}'";
            validateEventMapping(eventMapping!, identifier);
        }

        foreach (var key in characterNamesToChange)
        {
            var value = CharacterSpecificMapping[key];

            CharacterSpecificMapping.Remove(key);
            CharacterSpecificMapping["@" + key] = value;
        }
    }

    private static void validateEventMapping(EventMapping eventMapping, string identifier)
    {
        foreach (var (type, parameterConfig) in eventMapping)
        {
            if (!(parameterConfig!.Enabled ?? true)) continue;

            if (String.IsNullOrWhiteSpace(parameterConfig!.Parameter))
            {
                throw new ArgumentException($"{nameof(parameterConfig.Parameter)} for '{type}' of {identifier} must not be null or whitespace");
            }

            if (parameterConfig.Type.Equals(OscParameterType.Float))
            {
                if (parameterConfig.Min is null || parameterConfig.Max is null)
                {
                    throw new ArgumentException(
                        $"'{nameof(parameterConfig.Min)}' and '{nameof(parameterConfig.Max)}' for '{type}' of {identifier} are required for type '{OscParameterType.Float}'");
                }
            }
        }
    }
}

public class EventMapping : Dictionary<string, OscParameterConfig?>;