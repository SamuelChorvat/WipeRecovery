namespace WipeRecoveryApp.Models;

public class RestoreResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string TargetPath { get; set; } = string.Empty;
}
