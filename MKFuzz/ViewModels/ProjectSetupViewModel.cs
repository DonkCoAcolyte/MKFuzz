using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Platform.Storage;
using MKFuzz.Models;
using MKFuzz.Services;

namespace MKFuzz.ViewModels;

public partial class ProjectSetupViewModel : ViewModelBase
{
    private readonly DockerService _docker;

    private readonly MainWindowViewModel _mainVm;

    public MainWindowViewModel MainVm => _mainVm;

    [ObservableProperty]
    private FuzzingProject _project;

    public IStorageProvider? StorageProvider { get; set; }

    public Array StopConditions => Enum.GetValues(typeof(StopCondition));

    [ObservableProperty]
    private string _statusMessage = "";

    // Mount points (from central constants)
    public string MountSource => ContainerPaths.SourceMount;
    public string MountSeeds => ContainerPaths.SeedsMount;
    public string MountOutput => ContainerPaths.OutputMount;

    // Build command static parts
    public string FuzzBuildPrefix => ContainerPaths.FuzzBuildPrefix;
    public string FuzzBuildSuffix => ContainerPaths.FuzzBuildSuffix;
    public string CoverageBuildPrefix => ContainerPaths.CoverageBuildPrefix;
    public string CoverageBuildSuffix => ContainerPaths.CoverageBuildSuffix;

    public ProjectSetupViewModel(FuzzingProject project, DockerService docker, MainWindowViewModel mainVm)
    {
        _project = project;
        _docker = docker;
        _mainVm = mainVm;
    }

    [RelayCommand]
    private async Task BrowseSource()
    {
        if (StorageProvider == null) return;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Source Folder" });
        if (folders.Count > 0)
            Project.SourcePath = folders[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseSeeds()
    {
        if (StorageProvider == null) return;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Seeds Folder" });
        if (folders.Count > 0)
            Project.SeedsPath = folders[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseOutput()
    {
        if (StorageProvider == null) return;
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Output Folder" });
        if (folders.Count > 0)
            Project.OutputPath = folders[0].Path.LocalPath;
    }

    [RelayCommand]
    private async Task BrowseHarness()
    {
        if (StorageProvider == null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Harness File",
            AllowMultiple = false
        });
        if (files.Count > 0)
            Project.HarnessPath = files[0].Path.LocalPath;
    }

    [RelayCommand(CanExecute = nameof(CanStartContainer))]
    private async Task StartContainer()
    {
        try
        {
            StatusMessage = "Starting container...";
            var volumes = new Dictionary<string, string>
            {
                { Project.SourcePath, $"{ContainerPaths.SourceMount}:ro" },
                { Project.SeedsPath, $"{ContainerPaths.SeedsMount}:ro" },
                { Project.OutputPath, $"{ContainerPaths.OutputMount}:rw" }
            };
            await _docker.StartContainerAsync("fuzzing-image:latest", volumes);
            StatusMessage = "Container started.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
    private bool CanStartContainer() => !string.IsNullOrEmpty(Project.SourcePath);

    [RelayCommand]
    private void EditHarness()
    {
        if (!string.IsNullOrEmpty(Project.HarnessPath) && File.Exists(Project.HarnessPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Project.HarnessPath) { UseShellExecute = true });
        }
    }
}