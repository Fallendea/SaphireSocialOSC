using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;

namespace SaphireSocialOSC.model;

public class EventResponseBody
{
    public long Cursor { get; set; }
    public List<Event> Events { get; set; } = new();
}

public class Event
{
    public string Type { get; set; }
    public long Cursor { get; set; }

    public DateTimeOffset At { get; set; }

    public int CharacterId { get; set; }
    public string CharacterUsername { get; set; } = "";

    public int FromCharacterId { get; set; }
    public string FromCharacterName { get; set; } = "";

    //Only for dm received
    public long? ConversationId { get; set; } = null;

    //Only for dm received
    public string? Body { get; set; } = null;

    //Not there for every event type
    public string? TargetId { get; set; } = null;

    //Only there for money received event
    public float? Amount { get; set; } = null;
}