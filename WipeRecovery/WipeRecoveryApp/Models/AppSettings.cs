using System.Collections.Generic;

namespace WipeRecoveryApp.Models;

public class AppSettings
{
    public string WowRootPath { get; set; } = string.Empty;
    public string BackupFolder { get; set; } = string.Empty;
    public bool IncludeAddOns { get; set; } = false;
    public bool AutoUpdateEnabled { get; set; } = true;
    public bool CloudSyncEnabled { get; set; } = false;
    public Dictionary<string, bool> EnabledGameVersions { get; set; } = new();
}
