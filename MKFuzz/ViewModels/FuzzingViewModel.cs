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
    public override string Header => "Fuzzing";

    private readonly DockerService _docker;
    private readonly BuildService _build;
    private readonly FuzzingService _fuzzing;
    private readonly PostProcessService _postProcess;

    [ObservableProperty]
    private string _rawStats = "";

    [ObservableProperty]
    private FuzzingProject _project;

    [ObservableProperty]
    private string _statusMessage = "";

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
    private async Task BuildBinaries()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var fuzzSuccess = await _build.BuildFuzzTargetAsync(Project, progress);
        if (!fuzzSuccess)
        {
            StatusMessage = "Fuzz build failed.";
            return;
        }

        if (Project.GenerateCoverage)
        {
            var covSuccess = await _build.BuildCoverageTargetAsync(Project, progress);
            StatusMessage = covSuccess ? "Both builds completed successfully." : "Coverage build failed.";
        }
        else
        {
            StatusMessage = "Fuzz build completed (coverage disabled).";
        }
    }

    [RelayCommand]
    private async Task StartFuzzing()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var rawStats = new Progress<string>(raw => RawStats = raw);  // replaces previous content each time
        await _fuzzing.StartFuzzingAsync(Project, progress, rawStats);
        StatusMessage = "Fuzzing started.";
    }

    [RelayCommand]
    private async Task ResumeFuzzing()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var rawStats = new Progress<string>(raw => RawStats = raw);  // replaces previous content each time
        await _fuzzing.ResumeFuzzingAsync(Project, progress, rawStats);
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