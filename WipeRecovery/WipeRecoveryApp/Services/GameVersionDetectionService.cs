using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public class GameVersionDetectionService : IGameVersionDetectionService
{
    private static readonly Dictionary<string, string> KnownVersions = new()
    {
        { "_retail_", "Retail" },
        { "_classic_", "Classic" },
        { "_classic_era_", "Classic Era" },
        { "_ptr_", "Public Test Realm" }
    };

    public IEnumerable<GameVersionInfo> DetectVersions(string wowRootPath)
    {
        if (!Directory.Exists(wowRootPath))
            return [];

        return KnownVersions
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
                @"C:\Program Files (x86)\World of Warcraft"
            };

            return candidates.FirstOrDefault(Directory.Exists);
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return null;
        const string macPath = "/Applications/World of Warcraft";
        return Directory.Exists(macPath) ? macPath : null;

    }
}