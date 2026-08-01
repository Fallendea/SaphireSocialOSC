using System.Text.Json.Serialization;

namespace SaphireSocialOSC.model;

public class OscParameterConfig
{
    public bool? Enabled { get; set; } = true;
    public string? Parameter { get; set; }
    public OscParameterType Type { get; set; } = OscParameterType.Bool;
    public float? Min { get; set; }
    public float? Max { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OscParameterType
{
    Bool,
    Int,
    Float
}