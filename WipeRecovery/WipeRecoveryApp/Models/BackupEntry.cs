using System;
using WipeRecoveryApp.Constants;

namespace WipeRecoveryApp.Models;

public class BackupEntry
{
    public string Version { get; set; } = string.Empty; 
    public string FilePath { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }                  

    public string DisplayName => $"{WowVersions.SupportedVersions[Version]} - {Timestamp:MMMM d, yyyy - HH:mm}";
}
