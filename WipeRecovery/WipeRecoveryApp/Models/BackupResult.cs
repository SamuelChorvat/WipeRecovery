namespace WipeRecoveryApp.Models;

public class BackupResult
{
    public string GameVersion { get; set; } = string.Empty;
    public string BackupFilePath { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
