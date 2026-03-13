using MKFuzz.Models;
using MKFuzz.Services;
using System;
using System.Threading.Tasks;

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
        var cmd = $"cd /workspace/src && {project.BuildCommand} CC=afl-clang-fast CXX=afl-clang-fast++";
        var result = await _docker.ExecCommandAsync(cmd);
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
        var cmd = $"cd /workspace/src && {project.BuildCommand} CFLAGS=\"--coverage -O0 -g\" CXXFLAGS=\"--coverage -O0 -g\" LDFLAGS=\"--coverage\"";
        var result = await _docker.ExecCommandAsync(cmd);
        if (result.ExitCode != 0)
        {
            progress.Report($"Coverage build failed:\n{result.Stderr}");
            return false;
        }
        progress.Report("Coverage target built successfully.");
        return true;
    }
}