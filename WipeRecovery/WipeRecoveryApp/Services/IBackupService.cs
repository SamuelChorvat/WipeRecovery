using System.Threading.Tasks;
using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public interface IBackupService
{
    Task<BackupResult> Backup(string wowRootPath, string gameVersionFolder, 
        string backupDestination, bool includeAddOns);
}
