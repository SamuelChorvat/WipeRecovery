using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WipeRecoveryApp.Models;
using WipeRecoveryApp.Services;

namespace WipeRecoveryApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IGameVersionDetectionService _versionService;
    private readonly IBackupService _backupService;
    private readonly IRestoreService _restoreService;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isBusy;

    public ObservableCollection<GameVersionInfo> DetectedVersions { get; } = new();

    [ObservableProperty]
    private bool includeAddOns;

    [ObservableProperty]
    private string backupPath;

    public IRelayCommand BackupCommand { get; }
    public IRelayCommand<GameVersionInfo> RestoreCommand { get; }

    public MainViewModel(
        ISettingsService settingsService,
        IGameVersionDetectionService versionService,
        IBackupService backupService,
        IRestoreService restoreService)
    {
        _settingsService = settingsService;
        _versionService = versionService;
        _backupService = backupService;
        _restoreService = restoreService;

        var settings = _settingsService.Settings;

        includeAddOns = settings.IncludeAddOns;
        backupPath = settings.BackupFolder;

        BackupCommand = new AsyncRelayCommand(ExecuteBackupAsync, CanExecuteBackup);
        RestoreCommand = new AsyncRelayCommand<GameVersionInfo>(ExecuteRestoreAsync, _ => !IsBusy);

        LoadDetectedVersions();
    }

    private void LoadDetectedVersions()
    {
        DetectedVersions.Clear();
        var root = _settingsService.Settings.WowRootPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = _versionService.GetDefaultWowRootPath() ?? string.Empty;
            _settingsService.Settings.WowRootPath = root;
            _settingsService.Save();
        }

        var versions = _versionService.DetectVersions(root);
        foreach (var version in versions)
            DetectedVersions.Add(version);
    }

    private bool CanExecuteBackup() => !IsBusy;

    private async Task ExecuteBackupAsync()
    {
        IsBusy = true;
        StatusMessage = "Backing up...";

        var root = _settingsService.Settings.WowRootPath;
        var destination = BackupPath;
        var successCount = 0;

        foreach (var version in DetectedVersions.Where(v =>
            _settingsService.Settings.EnabledGameVersions.TryGetValue(v.FolderName, out var enabled) && enabled))
        {
            var result = await _backupService.Backup(root, version.FolderName, destination, IncludeAddOns);
            if (result.Success)
                successCount++;
            else
                StatusMessage = $"Backup failed: {result.ErrorMessage}";
        }

        StatusMessage = $"Backed up {successCount} version(s).";
        IsBusy = false;
    }

    private async Task ExecuteRestoreAsync(GameVersionInfo version)
    {
        // UI should handle zip selection and pass in version.
        // For now, this is just a placeholder for Restore flow.
        StatusMessage = "Restore started...";
        await Task.Delay(500); // simulate
        StatusMessage = $"Restore for {version.DisplayName} not implemented in UI yet.";
    }
}