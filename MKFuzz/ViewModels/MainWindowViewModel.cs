using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MKFuzz.Models;
using MKFuzz.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MKFuzz.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly DockerService _docker;
    private readonly DockerEnvironmentService _envService;
    private FuzzingProject _currentProject;

    public ObservableCollection<ViewModelBase> Tabs { get; } = new();

    [ObservableProperty]
    private string _dockerStatus = "Unknown";

    [ObservableProperty]
    private string _dockerStatusColor = "Gray";

    public IAsyncRelayCommand CheckEnvironmentCommand { get; }

    public MainWindowViewModel()
    {
        _currentProject = new FuzzingProject();
        _docker = new DockerService();
        _envService = new DockerEnvironmentService();

        Tabs.Add(new ProjectSetupViewModel(_currentProject, _docker, this));
        Tabs.Add(new FuzzingViewModel(_currentProject, _docker));
        Tabs.Add(new ResultsViewModel(_currentProject, _docker, this));

        CheckEnvironmentCommand = new AsyncRelayCommand(CheckEnvironmentAsync);
        // Optionally run check on startup
        Task.Run(async () => await CheckEnvironmentAsync());
    }

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

    [RelayCommand]
    private void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    public async ValueTask DisposeAsync()
    {
        if (!_currentProject.KeepContainerAlive)
            await _docker.StopContainerAsync();
    }
}