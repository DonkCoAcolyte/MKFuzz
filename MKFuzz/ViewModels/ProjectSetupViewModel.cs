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
    public override string Header => "Project Setup";

    private readonly DockerService _docker;

    private readonly MainWindowViewModel _mainVm;

    public MainWindowViewModel MainVm => _mainVm;

    [ObservableProperty]
    private FuzzingProject _project;

    public IStorageProvider? StorageProvider { get; set; }

    public Array StopConditions => Enum.GetValues(typeof(StopCondition));
    public Array BuildSystems => Enum.GetValues(typeof(BuildSystems));
    public Array InputTypes => Enum.GetValues(typeof(InputTypes));


    [ObservableProperty]
    private string _statusMessage = "";

    // Mount points (from central constants)
    public string SourceFolder => ContainerConfig.SourceFolder;
    public string MountSeeds => ContainerConfig.SeedsMount;
    public string MountOutput => ContainerConfig.OutputMount;

    public ProjectSetupViewModel(FuzzingProject project, DockerService docker, MainWindowViewModel mainVm)
    {
        Project = project;
        Project.FuzzBuildCommand = ContainerConfig.cmakeFuzzBuildCommand;
        Project.SanitizersBuildCommand = ContainerConfig.cmakeSanitizersBuildCommand;
        Project.CmplogBuildCommand = ContainerConfig.cmakeCmplogBuildCommand;
        Project.CoverageBuildCommand = ContainerConfig.cmakeCovBuildCommand;
        _docker = docker;
        _mainVm = mainVm;

        _mainVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsFuzzingContainerRunning))
                StartContainerCommand.NotifyCanExecuteChanged();
        };
    }

    // Handle project changes: detach from old, attach to new
    partial void OnProjectChanged(FuzzingProject? oldValue, FuzzingProject newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= OnProjectPropertyChanged;
        if (newValue != null)
        {
            newValue.PropertyChanged += OnProjectPropertyChanged;
            // Force re‑evaluation because the new project may already have a non‑empty SourcePath
            StartContainerCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnProjectPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FuzzingProject.SourcePath))
            StartContainerCommand.NotifyCanExecuteChanged();
        if (e.PropertyName == nameof(FuzzingProject.BuildSystem))
        {
            switch (Project.BuildSystem)
            {
                case Models.BuildSystems.cmake:
                    Project.FuzzBuildCommand = ContainerConfig.cmakeFuzzBuildCommand;
                    Project.SanitizersBuildCommand = ContainerConfig.cmakeSanitizersBuildCommand;
                    Project.CmplogBuildCommand = ContainerConfig.cmakeCmplogBuildCommand;
                    Project.CoverageBuildCommand = ContainerConfig.cmakeCovBuildCommand;
                    break;
                case Models.BuildSystems.raw:
                    Project.FuzzBuildCommand = ContainerConfig.rawFuzzBuildCommand;
                    Project.SanitizersBuildCommand = ContainerConfig.rawSanitizersBuildCommand;
                    Project.CmplogBuildCommand = ContainerConfig.rawCmplogBuildCommand;
                    Project.CoverageBuildCommand = ContainerConfig.rawCovBuildCommand;
                    break;
            }
        }
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
                { Project.SourcePath, $"{ContainerConfig.SourceMount}:ro" },
                { Project.SeedsPath, $"{ContainerConfig.SeedsMount}:ro" },
                { Project.OutputPath, $"{ContainerConfig.OutputMount}:rw" }
            };
            await _docker.StartContainerAsync("fuzzing-image:latest", volumes);
            MainVm.IsFuzzingContainerRunning = true;
            StatusMessage = "Container started.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
    private bool CanStartContainer() => !string.IsNullOrEmpty(Project.SourcePath) && !MainVm.IsFuzzingContainerRunning;

    [RelayCommand]
    private void EditHarness()
    {
        if (!string.IsNullOrEmpty(Project.HarnessPath) && File.Exists(Project.HarnessPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Project.HarnessPath) { UseShellExecute = true });
        }
    }
}