using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WipeRecoveryApp.Services;

public class StartupManager : IStartupManager
{
    private const string AppName = "WipeRecovery";

    public bool IsEnabled()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS requires a .plist file in ~/Library/LaunchAgents
            var path = GetMacPlistPath();
            return File.Exists(path);
        }

        return false;
    }

    public void Enable()
    {
        var exePath = Environment.ProcessPath ?? string.Empty;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.SetValue(AppName, $"\"{exePath}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var plist = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>{AppName}</string>
    <key>ProgramArguments</key>
    <array>
        <string>{exePath}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
</dict>
</plist>";
            var path = GetMacPlistPath();
            File.WriteAllText(path, plist);
        }
    }

    public void Disable()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue(AppName, false);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var path = GetMacPlistPath();
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private string GetMacPlistPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        return Path.Combine(home, "Library", "LaunchAgents", $"{AppName}.plist");
    }
}