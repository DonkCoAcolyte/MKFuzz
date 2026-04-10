using System;
using System.Threading.Tasks;
using MKFuzz.Models;
using MKFuzz.Services;  // for ContainerPaths

namespace MKFuzz.Services;

public class BuildService
{
    private readonly DockerService _docker;

    public BuildService(DockerService docker)
    {
        _docker = docker;
    }

    public async Task<bool> BuildFuzzTargetAsync(FuzzingProject project, IProgress<string> progress)
    {
        progress.Report("Building fuzz target...");
        // Build command: prefix + user command + suffix
        string fullCommand = $"{ContainerConfig.FuzzBuildPrefix}{project.FuzzBuildCommand}{ContainerConfig.FuzzBuildSuffix}";
        var result = await _docker.ExecCommandAsync(fullCommand);
        if (result.ExitCode != 0)
        {
            progress.Report($"Build failed:\n{result.Stderr}");
            return false;
        }
        progress.Report("Fuzz target built successfully.");
        return true;
    }

    public async Task<bool> BuildCoverageTargetAsync(FuzzingProject project, IProgress<string> progress)
    {
        if (!project.GenerateCoverage) return true;
        progress.Report("Building coverage target...");
        string fullCommand = $"{ContainerConfig.CoverageBuildPrefix}{project.CoverageBuildCommand}{ContainerConfig.CoverageBuildSuffix}";
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