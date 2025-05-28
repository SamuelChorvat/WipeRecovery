using System;
using System.Linq;
using System.Threading.Tasks;

namespace WipeRecoveryApp.Services;

public class AutoBackupService : IAutoBackupService
{
    private readonly IBackupService _backupService;
    private readonly ISettingsService _settingsService;
    private readonly IGameVersionDetectionService _versionDetection;

    public AutoBackupService(IBackupService backupService, ISettingsService settingsService, 
        IGameVersionDetectionService versionDetection)
    {
        _backupService = backupService;
        _settingsService = settingsService;
        _versionDetection = versionDetection;
    }

    public async Task RunIfDueAsync()
    {
        var settings = _settingsService.Settings;

        if (!settings.AutoBackupEnabled)
            return;

        var now = DateTime.UtcNow;
        if (settings.LastAutoBackupUtc.HasValue &&
            (now - settings.LastAutoBackupUtc.Value).TotalDays < settings.AutoBackupIntervalDays)
            return;

        var root = settings.WowRootPath;
        var backupPath = settings.BackupFolder;
        var includeAddOns = settings.IncludeAddOns;

        var versions = _versionDetection.DetectVersions(root)
            .Where(v => settings.EnabledGameVersions.TryGetValue(v.FolderName, out var selected) && selected);

        foreach (var version in versions)
        {
            await _backupService.Backup(root, version.FolderName, backupPath, includeAddOns);
        }

        settings.LastAutoBackupUtc = now;
        _settingsService.Save();
    }
}