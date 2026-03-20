using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MKFuzz.Models;
using MKFuzz.Services;

namespace MKFuzz.ViewModels;

public partial class FuzzingViewModel : ViewModelBase
{
    private readonly DockerService _docker;
    private readonly BuildService _build;
    private readonly FuzzingService _fuzzing;
    private readonly PostProcessService _postProcess;

    [ObservableProperty]
    private FuzzingProject _project;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private FuzzingStats _stats = new();

    public ObservableCollection<string> LogEntries { get; } = new();

    public FuzzingViewModel(FuzzingProject project, DockerService docker)
    {
        _project = project;
        _docker = docker;
        _build = new BuildService(docker);
        _fuzzing = new FuzzingService(docker);
        _postProcess = new PostProcessService(docker);
    }

    [RelayCommand]
    private async Task BuildFuzz()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var success = await _build.BuildFuzzTargetAsync(Project, progress);
        StatusMessage = success ? "Fuzz build done." : "Fuzz build failed.";
    }

    [RelayCommand]
    private async Task BuildCoverage()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var success = await _build.BuildCoverageTargetAsync(Project, progress);
        StatusMessage = success ? "Coverage build done." : "Coverage build failed.";
    }

    [RelayCommand]
    private async Task StartFuzzing()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var statsProgress = new Progress<FuzzingStats>(stats => Stats = stats);
        await _fuzzing.StartFuzzingAsync(Project, progress, statsProgress);
        StatusMessage = "Fuzzing started.";
    }

    [RelayCommand]
    private async Task ResumeFuzzing()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var statsProgress = new Progress<FuzzingStats>(stats => Stats = stats);
        await _fuzzing.ResumeFuzzingAsync(Project, progress, statsProgress);
        StatusMessage = "Resuming fuzzing...";
    }

    [RelayCommand]
    private async Task StopFuzzing()
    {
        await _fuzzing.StopFuzzingAsync();
        StatusMessage = "Fuzzing stopped.";
    }

    [RelayCommand]
    private async Task Analyze()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var success = await _postProcess.ProcessAsync(Project, progress);
        StatusMessage = success ? "Analysis complete." : "Analysis failed.";
    }
}