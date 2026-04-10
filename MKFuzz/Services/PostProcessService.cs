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
        var collectCmd = $"afl-collect -e /workspace/crashes_dedup/exploitable_gdb_script -d /workspace/crashes_dedup/crashes.db -r -rr -j {project.Cores} /workspace/sync /workspace/crashes_dedup -- {project.FuzzBinaryPath} {project.TargetArgs}";
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

        // Now we have /workspace/clean_corpus.cmin (minimized corpus) and /workspace/crashes_dedup (crashes)

        if (project.GenerateCoverage)
        {
            progress.Report("Generating coverage report...");
            // Create faux structure and run afl-cov-fast
            await _docker.ExecCommandAsync("mkdir -p /workspace/cov_corpus/SESSION000/queue");
            await _docker.ExecCommandAsync("cp -r /workspace/clean_corpus.cmin/* /workspace/cov_corpus/SESSION000/queue/");
            var covCmd = $"/opt/afl-cov-fast/afl-cov-fast.py -m llvm --code-dir /workspace/src --afl-fuzzing-dir /workspace/cov_corpus --coverage-cmd '{project.CovBinaryPath} {project.TargetArgs}' --binary-path {project.CovBinaryPath} -j{project.Cores}";
            var covResult = await _docker.ExecCommandAsync(covCmd);
            if (covResult.ExitCode != 0)
            {
                progress.Report($"afl-cov-fast failed:\n{covResult.Stderr}");
                return false;
            }
            // Copy report to host
            await _docker.ExecCommandAsync("cp -r /workspace/cov_corpus/cov/web /workspace/hostout/coverage_report");
            progress.Report("Coverage report saved to hostout/coverage_report");
            // Delete the faux structure
            await _docker.ExecCommandAsync("rm -rf /workspace/cov_corpus");
        }

        // Prepare final output for host
        if (project.SanitizeFilenames)
        {
            progress.Report("Sanitizing filenames...");

            // Copy to staging, sanitize, then copy to hostout
            await _docker.ExecCommandAsync("mkdir -p /workspace/staging");
            await _docker.ExecCommandAsync("cp -r /workspace/crashes_dedup /workspace/staging/"); //this is crashes
            await _docker.ExecCommandAsync("cp -r /workspace/clean_corpus.cmin /workspace/staging/minimized_corpus"); //this is corpus
            await _docker.ExecCommandAsync("crossrename -p /workspace/staging -r"); //crossrename
            await _docker.ExecCommandAsync("cp -r /workspace/staging/crashes_dedup /workspace/hostout/"); //pulled out crashes
            await _docker.ExecCommandAsync("cp -r /workspace/staging/minimized_corpus /workspace/hostout/"); //pulled out corpus
            await _docker.ExecCommandAsync("rm -rf /workspace/staging"); //delete the staging area
        }
        else
        {
            await _docker.ExecCommandAsync("cp -r /workspace/crashes_dedup /workspace/hostout/");
            await _docker.ExecCommandAsync("cp -r /workspace/clean_corpus.cmin /workspace/hostout/minimized_corpus");
        }

        progress.Report("Processing complete.");
        return true;
    }
}