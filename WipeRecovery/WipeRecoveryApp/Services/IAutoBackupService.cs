using System.Threading.Tasks;

namespace WipeRecoveryApp.Services;

public interface IAutoBackupService
{
    Task RunIfDueAsync();
}