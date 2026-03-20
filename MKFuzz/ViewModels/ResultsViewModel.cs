using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MKFuzz.Models;
using MKFuzz.Services;

namespace MKFuzz.ViewModels;

public partial class ResultsViewModel : ViewModelBase
{
    private readonly FuzzingProject _project; // keep reference
    private readonly DockerService _docker;
    private readonly MainWindowViewModel _mainVm;

    [ObservableProperty]
    private bool _hasCoverage;

    [ObservableProperty]
    private string _statusMessage = "";

    public ResultsViewModel(FuzzingProject project, DockerService docker, MainWindowViewModel mainVm)
    {
        _project = project;
        _docker = docker;
        _mainVm = mainVm;

        // Watch for OutputPath changes to update HasCoverage
        _project.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(FuzzingProject.OutputPath))
                CheckForCoverage();
        };
        CheckForCoverage();
    }

    private void CheckForCoverage()
    {
        if (string.IsNullOrEmpty(_project.OutputPath))
        {
            HasCoverage = false;
            return;
        }
        var coveragePath = Path.Combine(_project.OutputPath, "coverage_report", "index.html");
        HasCoverage = File.Exists(coveragePath);
    }

    [RelayCommand]
    private void OpenOutputFolder()
    {
        if (Directory.Exists(_project.OutputPath))
            Process.Start(new ProcessStartInfo(_project.OutputPath) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenCoverage()
    {
        var indexPath = Path.Combine(_project.OutputPath, "coverage_report", "index.html");
        if (File.Exists(indexPath))
            Process.Start(new ProcessStartInfo(indexPath) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task ExportArchive()
    {
        var zipPath = Path.Combine(_project.OutputPath, $"{_project.Name}_results.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);
        System.IO.Compression.ZipFile.CreateFromDirectory(_project.OutputPath, zipPath);
        StatusMessage = $"Archive created: {zipPath}";
    }

    [RelayCommand]
    private async Task StopContainer()
    {
        await _docker.StopContainerAsync();
        StatusMessage = "Container stopped.";
    }

    // No UpdateProject needed because the Results tab only uses the project's properties;
    // the reference is fixed, and we react to property changes via the subscription.
}