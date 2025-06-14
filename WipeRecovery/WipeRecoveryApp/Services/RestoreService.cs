using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public class RestoreService : IRestoreService
{
    public async Task<RestoreResult> Restore(string zipPath, string wowRootPath, string gameVersionFolder, bool restoreWtf, bool restoreAddOns)
    {
        var result = new RestoreResult();

        try
        {
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("Backup zip not found.", zipPath);

            var gameVersionPath = Path.Combine(wowRootPath, gameVersionFolder);
            if (!Directory.Exists(gameVersionPath))
                throw new DirectoryNotFoundException("WoW version folder not found.");

            // Create temp folder and extract
            var tempExtract = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            ZipFile.ExtractToDirectory(zipPath, tempExtract);

            if (restoreWtf)
            {
                var src = Path.Combine(tempExtract, "WTF");
                var dest = Path.Combine(gameVersionPath, "WTF");
                if (Directory.Exists(src))
                {
                    if (Directory.Exists(dest))
                        Directory.Delete(dest, true);

                    CopyDirectory(src, dest);
                }
            }

            if (restoreAddOns)
            {
                var src = Path.Combine(tempExtract, "Interface", "AddOns");
                var dest = Path.Combine(gameVersionPath, "Interface", "AddOns");

                if (Directory.Exists(src))
                {
                    if (Directory.Exists(dest))
                        Directory.Delete(dest, true);

                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    CopyDirectory(src, dest);
                }
            }

            Directory.Delete(tempExtract, true);

            result.TargetPath = gameVersionPath;
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