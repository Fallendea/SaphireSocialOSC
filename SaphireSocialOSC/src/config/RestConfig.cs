namespace SaphireSocialOSC.config;

public class RestConfig
{
    public string Host { get; set; } = "";
    public string Token { get; set; } = "";
    public int IntervalInSeconds { get; set; } = 60;
    public int TimeoutInSeconds { get; set; } = 10;
}