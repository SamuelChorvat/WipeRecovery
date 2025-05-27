using System.Threading.Tasks;
using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public interface IRestoreService
{
    Task<RestoreResult> Restore(string zipPath, string wowRootPath, string gameVersionFolder, 
        bool restoreWtf, bool restoreAddOns);
}