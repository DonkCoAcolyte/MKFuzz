using System;
using System.Text.Json.Serialization;

namespace MKFuzz.Models;

public enum StopCondition
{
    Manual,
    TimeBased,
    CrashCount
}

public class FuzzingProject
{
    public string Name { get; set; } = "New Project";
    public string SourcePath { get; set; } = "";
    public string HarnessPath { get; set; } = "";
    public string SeedsPath { get; set; } = "";
    public string OutputPath { get; set; } = "";

    public string BuildCommand { get; set; } = "make -j4";
    public string FuzzBinaryPath { get; set; } = "";   // full path inside container, e.g., /workspace/build_fuzz/bin/target
    public string CovBinaryPath { get; set; } = "";    // e.g., /workspace/build_cov/bin/target
    public string TargetArgs { get; set; } = "@@ /dev/null";

    public int Cores { get; set; } = Environment.ProcessorCount;
    public StopCondition StopWhen { get; set; } = StopCondition.TimeBased;
    public int StopValue { get; set; } = 3600;

    public bool GenerateCoverage { get; set; } = true;
    public bool KeepContainerAlive { get; set; } = false;
    public bool SanitizeFilenames { get; set; } = true;

    // Advanced
    public int MemoryLimit { get; set; } = 500;
    public int TimeoutMs { get; set; } = 1000;
    public string ExtraAflArgs { get; set; } = "";
}