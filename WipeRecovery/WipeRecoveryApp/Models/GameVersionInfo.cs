using CommunityToolkit.Mvvm.ComponentModel;

namespace WipeRecoveryApp.Models;

public partial class GameVersionInfo : ObservableObject
{
    public string FolderName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
