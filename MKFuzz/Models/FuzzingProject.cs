using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MKFuzz.Models;

public enum StopCondition
{
    Manual,
    CrashCount
}

public enum BuildSystems
{
    cmake,
    raw
}

public enum InputTypes
{
    undefined,
    ascii,
    binary
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
    private BuildSystems _buildSystem = BuildSystems.cmake;

    [ObservableProperty]
    private InputTypes _inputType = InputTypes.undefined;

    [ObservableProperty]
    private bool _useSanitizers = false;

    [ObservableProperty]
    private bool _useCMPLOG = false;

    [ObservableProperty]
    private string _fuzzBuildCommand = "make -j4";

    [ObservableProperty]
    private string _sanitizersBuildCommand = "make -j4"; // we will use ASAN (seems to be the most important one) and UBSAN (doesnt strike me as a necessary thing but its compatible with ASAN so shouldnt hurt)

    [ObservableProperty]
    private string _cmplogBuildCommand = "make -j4";

    [ObservableProperty]
    private string _coverageBuildCommand = "make -j4";

    [ObservableProperty]
    private string _fuzzBinaryPath = "";

    [ObservableProperty]
    private string _sanitizersBinaryPath = "";

    [ObservableProperty]
    private string _cmplogBinaryPath = "";

    [ObservableProperty]
    private string _covBinaryPath = "";

    [ObservableProperty]
    private string _targetArgs = "@@";

    [ObservableProperty]
    private int _cores = Environment.ProcessorCount;

    [ObservableProperty]
    private StopCondition _stopWhy = StopCondition.Manual;

    [ObservableProperty]
    private int _stopValue = 10;

    [ObservableProperty]
    private int _aflFuzzerStatsUpdateIntervalSeconds = 60;

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