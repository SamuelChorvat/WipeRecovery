using System;
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
    public bool AutoBackupEnabled { get; set; } = false;
    public int AutoBackupIntervalDays { get; set; } = 7;
    public DateTime? LastAutoBackupUtc { get; set; }
    public bool AutoRetentionEnabled { get; set; } = false;
    public int RetentionMaxPerVersion { get; set; } = 5;

}
