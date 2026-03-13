using MKFuzz.Models;
using MKFuzz.Services;
using System;
using System.Threading.Tasks;

namespace MKFuzz.Services;

public class PostProcessService
{
    private readonly DockerService _docker;

    public PostProcessService(DockerService docker)
    {
        _docker = docker;
    }

    public async Task<bool> ProcessAsync(FuzzingProject project, IProgress<string> progress)
    {
        progress.Report("Collecting crashes...");
        var collectCmd = $"afl-collect -e gdb_script -r -rr -j {project.Cores} /workspace/sync /workspace/hostout/crashes_dedup -- {project.FuzzBinaryPath} {project.TargetArgs}";
        var result = await _docker.ExecCommandAsync(collectCmd);
        if (result.ExitCode != 0)
        {
            progress.Report($"afl-collect failed:\n{result.Stderr}");
            return false;
        }

        progress.Report("Minimizing corpus...");
        var minimizeCmd = $"afl-minimize -c /workspace/clean_corpus --cmin --dry-run -j {project.Cores} /workspace/sync -- {project.FuzzBinaryPath} {project.TargetArgs}";
        result = await _docker.ExecCommandAsync(minimizeCmd);
        if (result.ExitCode != 0)
        {
            progress.Report($"afl-minimize failed:\n{result.Stderr}");
            return false;
        }

        await _docker.ExecCommandAsync("cp -r /workspace/clean_corpus.cmin /workspace/hostout/minimized_corpus");

        if (project.GenerateCoverage)
        {
            progress.Report("Generating coverage report...");
            var covCmd = $"/opt/afl-cov-fast/afl-cov-fast.py -m gcc --code-dir /workspace/src --afl-fuzzing-dir /workspace/clean_corpus.cmin --coverage-cmd '{project.CovBinaryPath} {project.TargetArgs}' -j {project.Cores}";
            result = await _docker.ExecCommandAsync(covCmd);
            if (result.ExitCode != 0)
            {
                progress.Report($"afl-cov-fast failed:\n{result.Stderr}");
                return false;
            }

            await _docker.ExecCommandAsync("cp -r /workspace/clean_corpus.cmin/cov/web /workspace/hostout/coverage_report");
            progress.Report("Coverage report saved to hostout/coverage_report");
        }

        if (project.SanitizeFilenames)
        {
            progress.Report("Sanitizing filenames...");
            await _docker.ExecCommandAsync("crossrename -p /workspace/hostout -r");
        }

        progress.Report("Processing complete.");
        return true;
    }
}