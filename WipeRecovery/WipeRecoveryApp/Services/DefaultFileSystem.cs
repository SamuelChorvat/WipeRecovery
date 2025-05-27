using System;
using System.IO;

namespace WipeRecoveryApp.Services;

public class DefaultFileSystem : IFileSystem
{
    private const string FileName = "settings.json";

    public string GetSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "WipeRecovery");
        return Path.Combine(appFolder, FileName);
    }

    public bool FileExists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    public void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }
}
