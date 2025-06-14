using System.Collections.Generic;

namespace WipeRecoveryApp.Constants;

public class WowVersions
{
    public static readonly Dictionary<string, string> SupportedVersions = new()
    {
        { "_retail_", "Retail" },
        { "_classic_", "Classic Progression" },
        { "_classic_era_", "Classic Era/Anniversary/SoD" },
    };
}