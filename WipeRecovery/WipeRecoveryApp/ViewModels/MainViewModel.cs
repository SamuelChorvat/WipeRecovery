using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WipeRecoveryApp.Models;
using WipeRecoveryApp.Services;

namespace WipeRecoveryApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public IRelayCommand BrowseFolderCommand { get; }
    
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
        
        BrowseFolderCommand = new AsyncRelayCommand(ExecuteBrowseFolderAsync);
        BackupCommand = new AsyncRelayCommand(ExecuteBackupAsync, CanExecuteBackup);
        RestoreCommand = new AsyncRelayCommand<GameVersionInfo>(ExecuteRestoreAsync, _ => !IsBusy);

        LoadDetectedVersions();
    }

    private void LoadDetectedVersions()
    {
        DetectedVersions.Clear();

        var root = EnsureWowRootPath();
        var savedSelections = _settingsService.Settings.EnabledGameVersions;
        var detected = _versionService.DetectVersions(root).ToList();

        CleanStaleSelections(savedSelections, detected);

        foreach (var version in detected)
        {
            version.IsSelected = savedSelections.TryGetValue(version.FolderName, out var sel) && sel;
            MonitorSelection(version, savedSelections);
            DetectedVersions.Add(version);
        }

        _settingsService.Save();
    }
    
    private bool CanExecuteBackup() => !IsBusy;

    private async Task ExecuteBackupAsync()
    {
        IsBusy = true;
        StatusMessage = "Backing up...";

        var root = _settingsService.Settings.WowRootPath;
        var destination = BackupPath;
        var successCount = 0;

        foreach (var version in DetectedVersions.Where(v => v.IsSelected))
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
    
    private async Task ExecuteBrowseFolderAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var window = desktop.MainWindow;
        if (window?.StorageProvider is not { CanPickFolder: true } provider)
            return;

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Backup Folder",
            AllowMultiple = false
        });

        var selected = folders.FirstOrDefault();
        if (selected != null)
        {
            BackupPath = selected.Path.LocalPath;
            _settingsService.Settings.BackupFolder = BackupPath;
            _settingsService.Save();
        }
    }
    
    private string EnsureWowRootPath()
    {
        var settings = _settingsService.Settings;

        if (!string.IsNullOrWhiteSpace(settings.WowRootPath))
            return settings.WowRootPath;

        var defaultPath = _versionService.GetDefaultWowRootPath() ?? string.Empty;
        settings.WowRootPath = defaultPath;
        _settingsService.Save();

        return defaultPath;
    }

    private void CleanStaleSelections(Dictionary<string, bool> savedSelections, List<GameVersionInfo> detected)
    {
        var detectedKeys = detected.Select(v => v.FolderName).ToHashSet();
        var stale = savedSelections.Keys.Except(detectedKeys).ToList();

        foreach (var key in stale)
            savedSelections.Remove(key);
    }

    private void MonitorSelection(GameVersionInfo version, Dictionary<string, bool> savedSelections)
    {
        version.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(version.IsSelected)) return;
            savedSelections[version.FolderName] = version.IsSelected;
            _settingsService.Save();
        };
    }
    
    partial void OnIncludeAddOnsChanged(bool value)
    {
        _settingsService.Settings.IncludeAddOns = value;
        _settingsService.Save();
    }
    
    partial void OnBackupPathChanged(string value)
    {
        _settingsService.Settings.BackupFolder = value;
        _settingsService.Save();
    }
}