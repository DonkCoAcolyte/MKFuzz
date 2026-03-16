using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MKFuzz.Models;

public enum StopCondition
{
    Manual,
    TimeBased,
    CrashCount
}

public partial class FuzzingProject : ObservableObject
{
    [ObservableProperty]
    private string _name = "New Project";

    [ObservableProperty]
    private string _sourcePath = "";

    [ObservableProperty]
    private string _harnessPath = "";

    [ObservableProperty]
    private string _seedsPath = "";

    [ObservableProperty]
    private string _outputPath = "";

    [ObservableProperty]
    private string _fuzzBuildCommand = "make -j4";

    [ObservableProperty]
    private string _coverageBuildCommand = "make -j4";

    [ObservableProperty]
    private string _fuzzBinaryPath = "";

    [ObservableProperty]
    private string _covBinaryPath = "";

    [ObservableProperty]
    private string _targetArgs = "@@ /dev/null";

    [ObservableProperty]
    private int _cores = Environment.ProcessorCount;

    [ObservableProperty]
    private StopCondition _stopWhen = StopCondition.TimeBased;

    [ObservableProperty]
    private int _stopValue = 3600;

    [ObservableProperty]
    private bool _generateCoverage = true;

    [ObservableProperty]
    private bool _keepContainerAlive = false;

    [ObservableProperty]
    private bool _sanitizeFilenames = true;

    [ObservableProperty]
    private int _memoryLimit = 500;

    [ObservableProperty]
    private int _timeoutMs = 1000;

    [ObservableProperty]
    private string _extraAflArgs = "";
}