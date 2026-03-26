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

    // State flags
    private bool _buildCompleted;
    private bool _fuzzingActive;
    private bool _sessionExists;   // true if a fuzzing session has been started (or resumed)

    [ObservableProperty]
    private string _rawStats = "";

    [ObservableProperty]
    private FuzzingProject _project;

    [ObservableProperty]
    private string _statusMessage = "";

    // Button enable states
    [ObservableProperty]
    private bool _buildEnabled = true;

    [ObservableProperty]
    private bool _startFuzzingEnabled = false;

    [ObservableProperty]
    private bool _resumeFuzzingEnabled = false;

    [ObservableProperty]
    private bool _analyzeEnabled = false;

    public ObservableCollection<string> LogEntries { get; } = new();

    public FuzzingViewModel(FuzzingProject project, DockerService docker)
    {
        _project = project;
        _docker = docker;
        _build = new BuildService(docker);
        _fuzzing = new FuzzingService(docker);
        _postProcess = new PostProcessService(docker);
    }

    // Called from MainWindowViewModel when the container is deleted or a new project is opened
    public void ResetState()
    {
        _buildCompleted = false;
        _fuzzingActive = false;
        _sessionExists = false;

        BuildEnabled = true;
        StartFuzzingEnabled = false;
        ResumeFuzzingEnabled = false;
        AnalyzeEnabled = false;
        RawStats = "";   // clear old stats
        StatusMessage = "";
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
            if (!covSuccess) return;
        }
        else
        {
            StatusMessage = "Fuzz build completed (coverage disabled).";
        }

        _buildCompleted = true;
        BuildEnabled = false;
        StartFuzzingEnabled = true;
    }

    [RelayCommand]
    private async Task StartFuzzing()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var rawStats = new Progress<string>(raw => RawStats = raw);
        await _fuzzing.StartFuzzingAsync(Project, progress, rawStats);

        _fuzzingActive = true;
        _sessionExists = true;
        StartFuzzingEnabled = false;
        ResumeFuzzingEnabled = false;
        AnalyzeEnabled = false;   // analysis only after stopping
        StatusMessage = "Fuzzing started.";
    }

    [RelayCommand]
    private async Task ResumeFuzzing()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var rawStats = new Progress<string>(raw => RawStats = raw);
        await _fuzzing.ResumeFuzzingAsync(Project, progress, rawStats);

        _fuzzingActive = true;
        StartFuzzingEnabled = false;
        ResumeFuzzingEnabled = false;
        AnalyzeEnabled = false;
        StatusMessage = "Resuming fuzzing...";
    }

    [RelayCommand]
    private async Task StopFuzzing()
    {
        await _fuzzing.StopFuzzingAsync();

        _fuzzingActive = false;
        if (_sessionExists)
        {
            ResumeFuzzingEnabled = true;
            AnalyzeEnabled = true;
        }
        StatusMessage = "Fuzzing stopped.";
    }

    [RelayCommand]
    private async Task Analyze()
    {
        var progress = new Progress<string>(msg => LogEntries.Add(msg));
        var success = await _postProcess.ProcessAsync(Project, progress);
        StatusMessage = success ? "Analysis complete." : "Analysis failed.";
        // Keep AnalyzeEnabled true (user can run analyze repeatedly)
    }
}