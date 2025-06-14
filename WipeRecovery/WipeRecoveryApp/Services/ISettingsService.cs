using WipeRecoveryApp.Models;

namespace WipeRecoveryApp.Services;

public interface ISettingsService
{
    AppSettings Settings { get; set; }
    void Load();
    void Save();
}
