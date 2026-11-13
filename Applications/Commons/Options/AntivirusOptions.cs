namespace Qwek.Applications.Commons.Options;

public class AntivirusOptions
{
    public const string SectionName = "Antivirus";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "clamav";
    public int Port { get; set; } = 3310;
    public int TimeoutSeconds { get; set; } = 30;
}
