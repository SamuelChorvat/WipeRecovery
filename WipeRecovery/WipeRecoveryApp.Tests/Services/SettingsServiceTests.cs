using FluentAssertions;
using WipeRecoveryApp.Models;
using WipeRecoveryApp.Services;

namespace WipeRecoveryApp.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public void Load_ShouldReturnDefaultSettings_WhenFileDoesNotExist()
    {
        var fs = new FakeFileSystem("/mock/path/settings.json");
        var service = new SettingsService(fs);

        service.Load();

        service.Settings.Should().NotBeNull();
        service.Settings.IncludeAddOns.Should().BeFalse();
    }

    [Fact]
    public void SaveAndLoad_ShouldPersistAndReloadSettings()
    {
        var fs = new FakeFileSystem("/mock/path/settings.json");
        var service = new SettingsService(fs);

        var expected = new AppSettings
        {
            WowRootPath = "/wow",
            BackupFolder = "/backups",
            IncludeAddOns = true,
            AutoUpdateEnabled = false,
            CloudSyncEnabled = true,
            EnabledGameVersions = new Dictionary<string, bool> { ["_retail_"] = true }
        };

        service.Settings = expected;
        service.Save();

        var newService = new SettingsService(fs);
        newService.Load();

        newService.Settings.Should().BeEquivalentTo(expected);
    }
}