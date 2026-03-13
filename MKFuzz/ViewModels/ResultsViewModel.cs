using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MKFuzz.Models;
using MKFuzz.Services;

namespace MKFuzz.ViewModels;

public partial class FileNode : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _fullPath = "";

    public ObservableCollection<FileNode> Children { get; } = new();
}

public partial class ResultsViewModel : ViewModelBase
{
    private readonly FuzzingProject _project;
    private readonly DockerService _docker;
    private readonly MainWindowViewModel _mainVm;

    [ObservableProperty]
    private ObservableCollection<FileNode> _folders = new();

    [ObservableProperty]
    private bool _hasCoverage;

    [ObservableProperty]
    private string _statusMessage = "";

    public ResultsViewModel(FuzzingProject project, DockerService docker, MainWindowViewModel mainVm)
    {
        _project = project;
        _docker = docker;
        _mainVm = mainVm;

        LoadFolders();
    }

    private void LoadFolders()
    {
        if (string.IsNullOrEmpty(_project.OutputPath) || !Directory.Exists(_project.OutputPath))
            return;

        var root = new FileNode { Name = "Output", FullPath = _project.OutputPath };
        foreach (var dir in Directory.GetDirectories(_project.OutputPath))
        {
            var node = new FileNode { Name = Path.GetFileName(dir), FullPath = dir };
            if (Path.GetFileName(dir) == "coverage_report")
                HasCoverage = true;
            root.Children.Add(node);
        }
        Folders.Clear();
        Folders.Add(root);
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
}