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
    private FuzzingProject _project;

    public IStorageProvider? StorageProvider { get; set; }

    public FuzzingProject Project
    {
        get => _project;
        set => SetProperty(ref _project, value);
    }

    public Array StopConditions => Enum.GetValues(typeof(StopCondition));

    [ObservableProperty]
    private string _statusMessage = "";

    // Forwarded Docker status from MainWindowViewModel
    public string DockerStatus => _mainVm.DockerStatus;
    public string DockerStatusColor => _mainVm.DockerStatusColor;
    public IAsyncRelayCommand CheckEnvironmentCommand => _mainVm.CheckEnvironmentCommand;

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
    private void SaveProject()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(Project);
        File.WriteAllText(Path.Combine(Project.OutputPath, $"{Project.Name}.fuzzproj"), json);
        StatusMessage = "Project saved.";
    }

    [RelayCommand(CanExecute = nameof(CanStartContainer))]
    private async Task StartContainer()
    {
        try
        {
            StatusMessage = "Starting container...";
            var volumes = new Dictionary<string, string>
            {
                { Project.SourcePath, "/workspace/src:ro" },
                { Project.SeedsPath, "/workspace/seeds:ro" },
                { Project.OutputPath, "/workspace/hostout:rw" }
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