using System.IO.Compression;
using FluentAssertions;
using WipeRecoveryApp.Services;

namespace WipeRecoveryApp.Tests.Services;

public class RestoreServiceTests
{
    [Fact]
    public async Task RestoreAsync_ShouldExtractAndRestoreWtf()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var backupFolder = Path.Combine(tempRoot, "_retail_");
        var wtf = Path.Combine(backupFolder, "WTF");
        Directory.CreateDirectory(wtf);
        await File.WriteAllTextAsync(Path.Combine(wtf, "test.wtf"), "data");

        var zipPath = Path.Combine(tempRoot, "test.zip");
        ZipFile.CreateFromDirectory(backupFolder, zipPath);

        var destRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(destRoot, "_retail_"));

        var service = new RestoreService();
        var result = await service.Restore(zipPath, destRoot, "_retail_", restoreWtf: true, restoreAddOns: false);

        result.Success.Should().BeTrue();
        File.Exists(Path.Combine(destRoot, "_retail_", "WTF", "test.wtf")).Should().BeTrue();
    }
}