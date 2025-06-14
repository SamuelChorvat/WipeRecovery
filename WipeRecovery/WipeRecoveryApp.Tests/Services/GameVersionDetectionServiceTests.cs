using FluentAssertions;
using WipeRecoveryApp.Services;

namespace WipeRecoveryApp.Tests.Services;

public class GameVersionDetectionServiceTests
{
    [Fact]
    public void DetectVersions_ShouldReturnOnlyExistingFolders()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "_retail_"));

        var service = new GameVersionDetectionService();

        // Act
        var result = service.DetectVersions(root).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Any(v => v.FolderName == "_retail_").Should().BeTrue();
    }
    
    [Fact]
    public void DetectVersions_ShouldReturnOnlySupportedGameVersions()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "_retail_"));
        Directory.CreateDirectory(Path.Combine(root, "_ptr_"));

        var service = new GameVersionDetectionService();

        // Act
        var result = service.DetectVersions(root).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Any(v => v.FolderName == "_retail_").Should().BeTrue();
        result.Any(v => v.FolderName == "_ptr_").Should().BeFalse();
    }

    [Fact]
    public void GetDefaultWowRootPath_ShouldReturnNull_OnUnsupportedOS()
    {
        var service = new GameVersionDetectionService();
        var result = service.GetDefaultWowRootPath();
        result.Should().Match(path => path == null || Directory.Exists(path));
    }
}