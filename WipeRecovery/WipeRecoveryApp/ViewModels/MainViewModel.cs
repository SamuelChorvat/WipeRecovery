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
    
    [ObservableProperty]
    private string _wowRootPath;
    
    public bool HasValidWowRoot => 
        !string.IsNullOrWhiteSpace(WowRootPath) && Directory.Exists(WowRootPath);

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
    
    public ObservableCollection<GameVersionInfo> DetectedVersions { get; } = [];

    public IRelayCommand BackupCommand { get; }
    public IRelayCommand RestoreSelectedCommand { get; }
    public IRelayCommand BrowseWowRootCommand { get; }


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

        _includeAddOns = settings.IncludeAddOns;
        _backupPath = settings.BackupFolder;
        
        BrowseFolderCommand = new AsyncRelayCommand(ExecuteBrowseFolderAsync);
        BackupCommand = new AsyncRelayCommand(ExecuteBackupAsync, CanExecuteBackup);
        RestoreSelectedCommand = new AsyncRelayCommand(ExecuteRestoreSelectedAsync, () => SelectedBackup != null);
        BrowseWowRootCommand = new AsyncRelayCommand(ExecuteBrowseWowRootAsync);
        WowRootPath = _settingsService.Settings.WowRootPath;

        LoadDetectedVersions();
        LoadAvailableBackups();
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
    
    private bool CanExecuteBackup()
    {
        return !IsBusy &&
               Directory.Exists(WowRootPath) &&
               Directory.Exists(BackupPath) &&
               DetectedVersions.Any(v => v.IsSelected);
    }


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
        LoadAvailableBackups();
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
        LoadAvailableBackups();
    }
    
    private void LoadAvailableBackups()
    {
        AvailableBackups.Clear();
        var folder = _settingsService.Settings.BackupFolder;

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        var zips = Directory.GetFiles(folder, "*.zip", SearchOption.TopDirectoryOnly);
        
        Console.WriteLine($"Scanning: {folder}");
        foreach (var path in zips)
            Console.WriteLine("Found file: " + path);

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
    }
    
    private async Task ExecuteRestoreSelectedAsync()
    {
        if (SelectedBackup is null)
        {
            StatusMessage = "No backup selected.";
            return;
        }

        var versionFolder = SelectedBackup.Version;
        var root = _settingsService.Settings.WowRootPath;

        StatusMessage = $"Restoring {SelectedBackup.DisplayName}...";

        var result = await _restoreService.Restore(
            SelectedBackup.FilePath,
            root,
            versionFolder,
            RestoreWtf,
            RestoreAddOns
        );

        StatusMessage = result.Success
            ? $"Restored backup for {versionFolder} successfully."
            : $"Restore failed: {result.ErrorMessage}";
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
        OnPropertyChanged(nameof(HasValidWowRoot));
    }
}