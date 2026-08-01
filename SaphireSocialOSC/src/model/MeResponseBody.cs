namespace SaphireSocialOSC.model;

public class MeResponseBody
{
    public int AccountId { get; set; }
    public int CharacterId { get; set; }
    public List<string> Scopes { get; set; } = new();
    public List<Character> Characters { get; set; } = new();
}

public class Character
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
}