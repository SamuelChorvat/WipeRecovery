using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WipeRecoveryApp.Services;

public class RetentionService : IRetentionService
{
    private readonly ISettingsService _settings;

    public RetentionService(ISettingsService settings)
    {
        _settings = settings;
    }

    public async Task CleanOldBackups()
    {
        var config = _settings.Settings;
        if (!config.AutoRetentionEnabled || config.RetentionMaxPerVersion < 1)
            return;

        var root = config.BackupFolder;
        if (!Directory.Exists(root)) return;

        var files = Directory.GetFiles(root, "WR~*~*.zip", SearchOption.TopDirectoryOnly);

        var grouped = files
            .Select(path => new { Path = path, Info = ParseBackup(path) })
            .Where(x => x.Info != null)
            .GroupBy(x => x.Info!.Value.Version);

        foreach (var group in grouped)
        {
            var sorted = group.OrderByDescending(x => x.Info!.Value.Timestamp).ToList();
            var toDelete = sorted.Skip(config.RetentionMaxPerVersion).ToList();

            foreach (var del in toDelete)
                File.Delete(del.Path);
        }
    }

    private (string Version, DateTime Timestamp)? ParseBackup(string path)
    {
        var file = Path.GetFileNameWithoutExtension(path);
        var parts = file.Split('~');
        if (parts.Length < 3) return null;

        var version = parts[1];
        var dateTime = parts[2];

        if (!DateTime.TryParseExact($"{dateTime}", "yyyy-MM-dd_HHmmss", null, DateTimeStyles.AssumeLocal, out var timestamp))
            return null;

        return (version, timestamp);
    }
}