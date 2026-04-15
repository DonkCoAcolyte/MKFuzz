using System;
using System.Threading.Tasks;
using MKFuzz.Models;

namespace MKFuzz.Services;

public class BuildService
{
    private readonly DockerService _docker;

    public BuildService(DockerService docker)
    {
        _docker = docker;
    }

    // Normal fuzz binary (instrumented, no sanitizers, no CMPLOG)
    public async Task<bool> BuildFuzzTargetAsync(FuzzingProject project, IProgress<string> progress)
    {
        progress.Report("Building fuzz target...");
        string fullCommand = project.FuzzBuildCommand;
        var result = await _docker.ExecCommandAsync(fullCommand);
        if (result.ExitCode != 0)
        {
            progress.Report($"Fuzz build failed:\n{result.Stderr}");
            return false;
        }
        progress.Report("Fuzz target built successfully.");
        return true;
    }

    // Sanitizer binary (ASAN+UBSAN)
    public async Task<bool> BuildSanitizersTargetAsync(FuzzingProject project, IProgress<string> progress)
    {
        if (!project.UseSanitizers) return true;
        progress.Report("Building sanitized binary...");
        string fullCommand = project.SanitizersBuildCommand;
        var result = await _docker.ExecCommandAsync(fullCommand);
        if (result.ExitCode != 0)
        {
            progress.Report($"Sanitized build failed:\n{result.Stderr}");
            return false;
        }
        progress.Report("Sanitized binary built successfully.");
        return true;
    }

    // CMPLOG binary
    public async Task<bool> BuildCmplogTargetAsync(FuzzingProject project, IProgress<string> progress)
    {
        if (!project.UseCMPLOG) return true;
        progress.Report("Building CMPLOG binary...");
        string fullCommand = project.CmplogBuildCommand;
        var result = await _docker.ExecCommandAsync(fullCommand);
        if (result.ExitCode != 0)
        {
            progress.Report($"CMPLOG build failed:\n{result.Stderr}");
            return false;
        }
        progress.Report("CMPLOG binary built successfully.");
        return true;
    }

    // Coverage binary (for coverage reports)
    public async Task<bool> BuildCoverageTargetAsync(FuzzingProject project, IProgress<string> progress)
    {
        if (!project.GenerateCoverage) return true;
        progress.Report("Building coverage target...");
        string fullCommand = project.CoverageBuildCommand;
        var result = await _docker.ExecCommandAsync(fullCommand);
        if (result.ExitCode != 0)
        {
            progress.Report($"Coverage build failed:\n{result.Stderr}");
            return false;
        }
        progress.Report("Coverage target built successfully.");
        return true;
    }
}