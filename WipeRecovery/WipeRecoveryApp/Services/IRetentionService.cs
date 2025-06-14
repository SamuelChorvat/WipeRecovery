using System.Threading.Tasks;

namespace WipeRecoveryApp.Services;

public interface IRetentionService
{
    Task CleanOldBackups();
}