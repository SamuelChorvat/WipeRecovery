using System.IO;
using System.Text.Json;
using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public class SettingsService : ISettingsService
{
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _serializerOptions;

    public AppSettings Settings { get; set; } = new();

    public SettingsService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public void Load()
    {
        var path = _fileSystem.GetSettingsFilePath();
        if (!_fileSystem.FileExists(path)) return;
        var json = _fileSystem.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<AppSettings>(json);
        if (loaded != null)
            Settings = loaded;
    }

    public void Save()
    {
        var path = _fileSystem.GetSettingsFilePath();
        var dir = Path.GetDirectoryName(path)!;
        _fileSystem.EnsureDirectoryExists(dir);
        var json = JsonSerializer.Serialize(Settings, _serializerOptions);
        _fileSystem.WriteAllText(path, json);
    }
}
