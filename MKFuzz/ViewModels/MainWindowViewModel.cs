using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MKFuzz.Models;
using MKFuzz.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace MKFuzz.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly DockerService _docker;
    private readonly DockerEnvironmentService _envService;
    private FuzzingProject _currentProject;
    private ProjectSetupViewModel _projectSetupVm;
    private FuzzingViewModel _fuzzingVm;
    private ResultsViewModel _resultsVm;

    public FuzzingViewModel FuzzingVm => _fuzzingVm;
    public ObservableCollection<ViewModelBase> Tabs { get; } = new();

    [ObservableProperty]
    private string _dockerStatus = "Unknown";

    [ObservableProperty]
    private string _dockerStatusColor = "Gray";

    [ObservableProperty]
    private bool _isFuzzingContainerRunning;

    public IStorageProvider? StorageProvider { get; set; }

    public MainWindowViewModel()
    {
        IsFuzzingContainerRunning = false;
        _currentProject = new FuzzingProject();
        _docker = new DockerService();
        _envService = new DockerEnvironmentService();

        var projectSetupVm = new ProjectSetupViewModel(_currentProject, _docker, this);
        _projectSetupVm = projectSetupVm;
        Tabs.Add(projectSetupVm);

        var fuzzingVm = new FuzzingViewModel(_currentProject, _docker);
        _fuzzingVm = fuzzingVm;
        Tabs.Add(fuzzingVm);

        var resultsVm = new ResultsViewModel(_currentProject, _docker, this);
        _resultsVm = resultsVm;
        Tabs.Add(resultsVm);

        Task.Run(async () => await CheckEnvironmentAsync());
    }

    // --- Docker environment check (unchanged) ---
    [RelayCommand]
    private async Task CheckEnvironmentAsync()
    {
        DockerStatus = "Checking Docker...";
        DockerStatusColor = "Orange";

        if (!await _envService.IsDockerRunningAsync())
        {
            DockerStatus = "Docker not running!";
            DockerStatusColor = "Red";
            return;
        }

        if (await _envService.IsCorePatternSetCorrectlyAsync())
        {
            DockerStatus = "Docker ready (core_pattern OK)";
            DockerStatusColor = "Green";
        }
        else
        {
            DockerStatus = "core_pattern not set – attempting to fix...";
            DockerStatusColor = "Orange";
            if (await _envService.SetCorePatternAsync())
            {
                DockerStatus = "core_pattern set successfully";
                DockerStatusColor = "Green";
            }
            else
            {
                DockerStatus = "Failed to set core_pattern – manual intervention may be needed";
                DockerStatusColor = "Red";
            }
        }
    }

    // --- File menu commands (correctly implemented) ---
    [RelayCommand]
    private void NewProject()
    {
        var newProj = new FuzzingProject();
        _currentProject = newProj;
        _projectSetupVm.Project = newProj;
        _fuzzingVm.Project = newProj;
        _resultsVm.Project = newProj;
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        if (StorageProvider == null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Fuzzing Project",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Fuzzing Project") { Patterns = new[] { "*.fuzzproj" } } }
        });
        if (files.Count > 0)
        {
            var path = files[0].Path.LocalPath;
            var json = await File.ReadAllTextAsync(path);
            var proj = System.Text.Json.JsonSerializer.Deserialize<FuzzingProject>(json);
            if (proj != null)
            {
                _currentProject = proj;
                _projectSetupVm.Project = proj;
                _fuzzingVm.Project = proj;
                _resultsVm.Project = proj;
            }
        }
    }

    [RelayCommand]
    private async Task SaveProject()
    {
        if (StorageProvider == null) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Fuzzing Project",
            DefaultExtension = "fuzzproj",
            FileTypeChoices = new[] { new FilePickerFileType("Fuzzing Project") { Patterns = new[] { "*.fuzzproj" } } }
        });
        if (file != null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_currentProject);
            await File.WriteAllTextAsync(file.Path.LocalPath, json);
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_currentProject.KeepContainerAlive)
            await _docker.DeleteContainerAsync();
    }
}