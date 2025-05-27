using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WipeRecoveryApp.Constants;
using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public class GameVersionDetectionService : IGameVersionDetectionService
{
    public IEnumerable<GameVersionInfo> DetectVersions(string wowRootPath)
    {
        if (!Directory.Exists(wowRootPath))
            return [];

        return WowVersions.SupportedVersions
            .Where(kv => Directory.Exists(Path.Combine(wowRootPath, kv.Key)))
            .Select(kv => new GameVersionInfo
            {
                FolderName = kv.Key,
                DisplayName = kv.Value,
                FullPath = Path.Combine(wowRootPath, kv.Key)
            });
    }

    public string? GetDefaultWowRootPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var candidates = new[]
            {
                @"C:\Program Files\World of Warcraft",
                @"C:\Program Files (x86)\World of Warcraft",
                @"C:\Program Files (x86)\Battle.net\World of Warcraft",
                @"C:\Program Files\Battle.net\World of Warcraft"
            };

            return candidates.FirstOrDefault(Directory.Exists);
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return null;
        {
            var candidates = new[]
            {
                "/Applications/World of Warcraft",
                "/Users/Shared/World of Warcraft",
                "/Applications/Battle.net/World of Warcraft"
            };

            return candidates.FirstOrDefault(Directory.Exists);
        }

    }
}