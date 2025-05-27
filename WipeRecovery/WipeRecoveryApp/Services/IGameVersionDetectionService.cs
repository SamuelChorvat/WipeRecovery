using System.Collections.Generic;
using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public interface IGameVersionDetectionService
{
    IEnumerable<GameVersionInfo> DetectVersions(string wowRootPath);
    string? GetDefaultWowRootPath();
}