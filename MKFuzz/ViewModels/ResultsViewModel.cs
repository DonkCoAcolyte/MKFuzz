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
    public override string Header => "Results";

    private readonly DockerService _docker;
    private readonly MainWindowViewModel _mainVm;

    [ObservableProperty]
    private FuzzingProject _project;

    [ObservableProperty]
    private bool _hasCoverage;

    [ObservableProperty]
    private string _statusMessage = "";

    public ResultsViewModel(FuzzingProject project, DockerService docker, MainWindowViewModel mainVm)
    {
        _docker = docker;
        _mainVm = mainVm;
        // Use the property setter to trigger OnProjectChanged automatically
        Project = project;
        // CheckForCoverage will be called inside OnProjectChanged, but we call it once here too for safety
        CheckForCoverage();
    }

    partial void OnProjectChanged(FuzzingProject? oldValue, FuzzingProject newValue)
    {
        if (oldValue != null)
            oldValue.PropertyChanged -= Project_PropertyChanged;
        if (newValue != null)
            newValue.PropertyChanged += Project_PropertyChanged;

        // Refresh coverage status whenever the project instance changes
        CheckForCoverage();
    }

    private void Project_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FuzzingProject.OutputPath) ||
            e.PropertyName == nameof(FuzzingProject.GenerateCoverage))
            CheckForCoverage();
    }

    public void CheckForCoverage()
    {
        if (!Project.GenerateCoverage)
        {
            HasCoverage = false;
            return;
        }

        if (string.IsNullOrEmpty(Project.OutputPath))
        {
            HasCoverage = false;
            return;
        }

        var indexPath = Path.Combine(Project.OutputPath, "coverage_report", "index.html");
        HasCoverage = File.Exists(indexPath);
    }

    [RelayCommand]
    private void OpenOutputFolder()
    {
        if (Directory.Exists(Project.OutputPath))
            Process.Start(new ProcessStartInfo(Project.OutputPath) { UseShellExecute = true });
    }

    [RelayCommand]
    private void OpenCoverage()
    {
        var indexPath = Path.Combine(Project.OutputPath, "coverage_report", "index.html");
        if (File.Exists(indexPath))
            Process.Start(new ProcessStartInfo(indexPath) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task DeleteContainer()
    {
        await _docker.DeleteContainerAsync();
        _mainVm.IsFuzzingContainerRunning = false;
        _mainVm.FuzzingVm.ResetState();
        StatusMessage = "Container deleted.";
    }
}