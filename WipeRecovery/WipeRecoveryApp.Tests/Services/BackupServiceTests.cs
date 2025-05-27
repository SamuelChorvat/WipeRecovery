using FluentAssertions;
using WipeRecoveryApp.Services;

namespace WipeRecoveryApp.Tests.Services;

public class BackupServiceTests
{
    [Fact]
    public async Task BackupAsync_ShouldCreateZip_WhenValid()
    {
        var wowRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        const string version = "_retail_";
        var wtf = Path.Combine(wowRoot, version, "WTF");
        Directory.CreateDirectory(wtf);
        await File.WriteAllTextAsync(Path.Combine(wtf, "config.wtf"), "test");

        var backupFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(backupFolder);

        var service = new BackupService();

        var result = await service.Backup(wowRoot, version, backupFolder, includeAddOns: false);

        result.Success.Should().BeTrue();
        result.BackupFilePath.Should().EndWith(".zip");
        File.Exists(result.BackupFilePath).Should().BeTrue();
    }
}