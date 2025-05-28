using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
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
    private readonly IAutoBackupService _autoBackupService;
    private readonly IRetentionService _retentionService;
    
    [ObservableProperty]
    private string _wowRootPath;
    
    public bool HasValidWowRoot => 
        !string.IsNullOrWhiteSpace(WowRootPath) && Directory.Exists(WowRootPath);
    
    public bool ShowNoVersionsWarning =>
        HasValidWowRoot && DetectedVersions.Count == 0;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _includeAddOns;

    [ObservableProperty]
    private string _backupPath;
    
    [ObservableProperty]
    private BackupEntry? _selectedBackup;

    [ObservableProperty]
    private bool _restoreAddOns = true;

    [ObservableProperty]
    private bool _restoreWtf = true;
    
    [ObservableProperty]
    private ObservableCollection<BackupEntry> _availableBackups = [];
    
    [ObservableProperty]
    private ObservableCollection<GameVersionInfo> _detectedVersions = [];
    
    [ObservableProperty]
    private bool _autoBackupEnabled;

    [ObservableProperty]
    private string _autoBackupIntervalDays;

    [ObservableProperty]
    private bool _autoRetentionEnabled;

    [ObservableProperty]
    private string _retentionMaxPerVersion;

    public IRelayCommand BackupCommand { get; }
    public IRelayCommand RestoreSelectedCommand { get; }
    public IRelayCommand BrowseWowRootCommand { get; }


    public MainViewModel(
        ISettingsService settingsService,
        IGameVersionDetectionService versionService,
        IBackupService backupService,
        IRestoreService restoreService,
        IAutoBackupService autoBackupService,
        IRetentionService retentionService)
    {
        _settingsService = settingsService;
        _versionService = versionService;
        _backupService = backupService;
        _restoreService = restoreService;
        _autoBackupService = autoBackupService;
        _retentionService = retentionService;

        var settings = _settingsService.Settings;

        _includeAddOns = settings.IncludeAddOns;
        _backupPath = settings.BackupFolder;
        
        BrowseFolderCommand = new AsyncRelayCommand(ExecuteBrowseFolderAsync);
        BackupCommand = new AsyncRelayCommand(ExecuteBackupAsync, CanExecuteBackup);
        RestoreSelectedCommand = new AsyncRelayCommand(ExecuteRestoreSelectedAsync, CanExecuteRestoreSelected);
        BrowseWowRootCommand = new AsyncRelayCommand(ExecuteBrowseWowRootAsync);
        WowRootPath = _settingsService.Settings.WowRootPath;
        AutoBackupEnabled = settings.AutoBackupEnabled;
        AutoBackupIntervalDays = settings.AutoBackupIntervalDays.ToString();
        AutoRetentionEnabled = settings.AutoRetentionEnabled;
        RetentionMaxPerVersion = settings.RetentionMaxPerVersion.ToString();

        LoadDetectedVersions();
        LoadAvailableBackups();
        _ = RunStartupMaintenanceAsync();
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
        OnPropertyChanged(nameof(ShowNoVersionsWarning));
    }
    
    private bool CanExecuteBackup()
    {
        return !IsBusy &&
               Directory.Exists(WowRootPath) &&
               Directory.Exists(BackupPath) &&
               DetectedVersions.Any(v => v.IsSelected);
    }
    
    private bool CanExecuteRestoreSelected()
    {
        return !IsBusy &&
               Directory.Exists(WowRootPath) &&
               Directory.Exists(BackupPath) &&
               SelectedBackup != null;
    }

    private async Task ExecuteBackupAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Backing up...";

        var root = WowRootPath;
        var destination = BackupPath;
        var selected = DetectedVersions.Where(v => v.IsSelected).ToList();
        var includeAddOns = IncludeAddOns;

        var results = await Task.Run(() =>
        {
            var successCount = 0;

            foreach (var version in selected)
            {
                var result = _backupService.Backup(root, version.FolderName, destination, includeAddOns).Result;
                if (result.Success)
                    successCount++;
                else
                    StatusMessage = $"Backup failed: {result.ErrorMessage}";
            }

            return successCount;
        });
        
        await _retentionService.CleanOldBackups();
        StatusMessage = $"Backed up {results} version(s).";
        LoadAvailableBackups();
        IsBusy = false;
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
        {
            WowRootPath = settings.WowRootPath;
            return settings.WowRootPath;
        }

        var defaultPath = _versionService.GetDefaultWowRootPath();
        if (string.IsNullOrWhiteSpace(defaultPath)) return string.Empty;
        settings.WowRootPath = defaultPath;
        WowRootPath = defaultPath;
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
            BackupCommand.NotifyCanExecuteChanged();
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
        BackupCommand.NotifyCanExecuteChanged();
        RestoreSelectedCommand.NotifyCanExecuteChanged();
        LoadAvailableBackups();
    }
    
    private void LoadAvailableBackups()
    {
        AvailableBackups.Clear();
        var folder = _settingsService.Settings.BackupFolder;

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        var zips = Directory.GetFiles(folder, "*.zip", SearchOption.TopDirectoryOnly);

        foreach (var path in zips)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);

            // Example: WR~_retail_~2025-05-27_140212
            var parts = fileName.Split('~');
            if (parts.Length < 3 || !fileName.StartsWith("WR~")) continue;

            var version = parts[1];
            var dateStr = parts[2];

            if (DateTime.TryParseExact($"{dateStr}", "yyyy-MM-dd_HHmmss", null, DateTimeStyles.AssumeLocal, out var timestamp))
            {
                AvailableBackups.Add(new BackupEntry
                {
                    Version = version,
                    Timestamp = timestamp,
                    FilePath = path
                });
            }
        }

        // Sort most recent first
        var sorted = AvailableBackups.OrderByDescending(b => b.Timestamp).ToList();
        AvailableBackups = new ObservableCollection<BackupEntry>(sorted);
        SelectedBackup = AvailableBackups.FirstOrDefault();
        RestoreSelectedCommand.NotifyCanExecuteChanged();
    }
    
    private async Task ExecuteRestoreSelectedAsync()
    {
        if (SelectedBackup == null || IsBusy) return;

        IsBusy = true;
        StatusMessage = $"Restoring {SelectedBackup.DisplayName}...";

        var backup = SelectedBackup;
        var wtf = RestoreWtf;
        var addons = RestoreAddOns;
        var wowRoot = WowRootPath;
        var version = backup.Version;

        var result = await Task.Run(() =>
            _restoreService.Restore(backup.FilePath, wowRoot, version, wtf, addons).Result);

        StatusMessage = result.Success
            ? $"Restored {backup.DisplayName} successfully."
            : $"Restore failed: {result.ErrorMessage}";

        IsBusy = false;
    }
    
    private async Task ExecuteBrowseWowRootAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var window = desktop.MainWindow;
        if (window?.StorageProvider is not { CanPickFolder: true } provider)
            return;

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select WoW Installation Folder",
            AllowMultiple = false
        });

        var selected = folders.FirstOrDefault();
        if (selected != null)
        {
            WowRootPath = selected.Path.LocalPath;
            _settingsService.Settings.WowRootPath = WowRootPath;
            _settingsService.Save();

            LoadDetectedVersions();
        }
    }

    partial void OnWowRootPathChanged(string value)
    {    
        _settingsService.Settings.WowRootPath = value;
        _settingsService.Save();
        BackupCommand.NotifyCanExecuteChanged();
        RestoreSelectedCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(HasValidWowRoot));
        OnPropertyChanged(nameof(ShowNoVersionsWarning)); 
    }
    
    partial void OnIsBusyChanged(bool value)
    {
        BackupCommand.NotifyCanExecuteChanged();
        RestoreSelectedCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedBackupChanged(BackupEntry? value)
    {
        RestoreSelectedCommand.NotifyCanExecuteChanged();
    }
    
    private async Task RunStartupMaintenanceAsync()
    {
        await _autoBackupService.RunIfDueAsync();
        await _retentionService.CleanOldBackups();
        LoadAvailableBackups();
    }
    
    partial void OnAutoBackupEnabledChanged(bool value)
    {
        _settingsService.Settings.AutoBackupEnabled = value;
        _settingsService.Save();
    }

    partial void OnAutoBackupIntervalDaysChanged(string value)
    {
        if (!int.TryParse(value, out var parsed)) return;
        AutoBackupIntervalDays = parsed.ToString();
        _settingsService.Settings.AutoBackupIntervalDays = parsed;
        _settingsService.Save();
    }

    partial void OnAutoRetentionEnabledChanged(bool value)
    {
        _settingsService.Settings.AutoRetentionEnabled = value;
        _settingsService.Save();
    }

    partial void OnRetentionMaxPerVersionChanged(string value)
    {
        if (!int.TryParse(value, out var parsed)) return;
        RetentionMaxPerVersion = parsed.ToString();
        _settingsService.Settings.RetentionMaxPerVersion = parsed;
        _settingsService.Save();
    }
}