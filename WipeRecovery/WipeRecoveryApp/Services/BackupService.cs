using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public class BackupService : IBackupService
{
    public async Task<BackupResult> Backup(string wowRootPath, string gameVersionFolder, 
        string backupDestination, bool includeAddOns)
    {
        var result = new BackupResult { GameVersion = gameVersionFolder };

        try
        {
            // Validate paths
            var gameVersionPath = Path.Combine(wowRootPath, gameVersionFolder);
            var wtfPath = Path.Combine(gameVersionPath, "WTF");
            var addonsPath = Path.Combine(gameVersionPath, "Interface", "AddOns");

            if (!Directory.Exists(wtfPath))
                throw new DirectoryNotFoundException($"WTF folder not found for {gameVersionFolder}");

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            var zipName = $"WR~{gameVersionFolder}~{timestamp}.zip";
            var zipPath = Path.Combine(backupDestination, zipName);

            // Ensure backup directory
            Directory.CreateDirectory(backupDestination);

            // Create temp folder
            var tempFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempFolder);

            var tempWtf = Path.Combine(tempFolder, "WTF");
            CopyDirectory(wtfPath, tempWtf);

            if (includeAddOns && Directory.Exists(addonsPath))
            {
                var tempAddons = Path.Combine(tempFolder, "Interface", "AddOns");
                Directory.CreateDirectory(Path.GetDirectoryName(tempAddons)!);
                CopyDirectory(addonsPath, tempAddons);
            }

            ZipFile.CreateFromDirectory(tempFolder, zipPath);
            Directory.Delete(tempFolder, true);

            result.BackupFilePath = zipPath;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dest = Path.Combine(destinationDir, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dest = Path.Combine(destinationDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }
}