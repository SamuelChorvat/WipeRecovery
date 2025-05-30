namespace WipeRecoveryApp.Services;

public interface IStartupManager
{
    bool IsEnabled();
    void Enable();
    void Disable();
}
