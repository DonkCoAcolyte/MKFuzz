using MKFuzz.Models;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MKFuzz.Services;

public class FuzzingService
{
    private int AFL_FUZZER_STATS_UPDATE_INTERVAL = 60 * 1000;

    private readonly DockerService _docker;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    public FuzzingService(DockerService docker)
    {
        _docker = docker;
    }

    public async Task StartFuzzingAsync(FuzzingProject project, IProgress<string> progress, IProgress<string> rawStats, Action onCrashThresholdReached)
    {
        AFL_FUZZER_STATS_UPDATE_INTERVAL = project.AflFuzzerStatsUpdateIntervalSeconds * 1000;
        var config = new
        {
            target = project.FuzzBinaryPath,
            cmdline = project.TargetArgs,
            input = "/workspace/seeds",
            output = "/workspace/sync",
            mem_limit = project.MemoryLimit.ToString(),
            timeout = project.TimeoutMs.ToString()
        };
        var configJson = JsonSerializer.Serialize(config);
        await _docker.ExecCommandAsync($"echo '{configJson}' > /workspace/fuzz.json");

        var fuzzCmd = $"export AFL_FUZZER_STATS_UPDATE_INTERVAL={project.AflFuzzerStatsUpdateIntervalSeconds} && cd /workspace && afl-multicore -c fuzz.json start {project.Cores}";
        _ = _docker.ExecCommandAsync(fuzzCmd);

        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    await Task.Delay(AFL_FUZZER_STATS_UPDATE_INTERVAL, _cts.Token);
                    var result = await _docker.ExecCommandAsync("afl-whatsup -s /workspace/sync");
                    if (result.ExitCode == 0)
                    {
                        rawStats.Report(result.Stdout);

                        // Check crash count condition
                        if (project.StopWhy == StopCondition.CrashCount)
                        {
                            int crashes = ParseCrashes(result.Stdout);
                            if (crashes >= project.StopValue)
                            {
                                onCrashThresholdReached?.Invoke();
                                break; // exit loop – fuzzer will be stopped by the action
                            }
                        }
                    }
                    else
                    {
                        progress.Report($"afl-whatsup error: {result.Stderr}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested – exit cleanly
            }
        });
    }

    public async Task ResumeFuzzingAsync(FuzzingProject project, IProgress<string> progress, IProgress<string> rawStats, Action onCrashThresholdReached)
    {
        var checkCmd = "test -d /workspace/sync && echo exists";
        var check = await _docker.ExecCommandAsync(checkCmd);
        if (check.ExitCode != 0)
        {
            progress.Report("No existing fuzzing session found. Start a new one first.");
            return;
        }

        var config = new
        {
            target = project.FuzzBinaryPath,
            cmdline = project.TargetArgs,
            input = "/workspace/seeds",
            output = "/workspace/sync",
            afl_margs = $"-u {project.AflFuzzerStatsUpdateIntervalSeconds}",
            mem_limit = project.MemoryLimit.ToString(),
            timeout = project.TimeoutMs.ToString()
        };
        var configJson = JsonSerializer.Serialize(config);
        await _docker.ExecCommandAsync($"echo '{configJson}' > /workspace/fuzz.json");

        var fuzzCmd = $"cd /workspace && afl-multicore -c fuzz.json resume {project.Cores}";
        _ = _docker.ExecCommandAsync(fuzzCmd);

        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(async () =>
        {
            try
            {
                while (true)
                {
                    await Task.Delay(AFL_FUZZER_STATS_UPDATE_INTERVAL, _cts.Token);
                    var result = await _docker.ExecCommandAsync("afl-whatsup -s /workspace/sync");
                    if (result.ExitCode == 0)
                    {
                        rawStats.Report(result.Stdout);

                        if (project.StopWhy == StopCondition.CrashCount)
                        {
                            int crashes = ParseCrashes(result.Stdout);
                            if (crashes >= project.StopValue)
                            {
                                onCrashThresholdReached?.Invoke();
                                break;
                            }
                        }
                    }
                    else
                    {
                        progress.Report($"afl-whatsup error: {result.Stderr}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested – exit cleanly
            }
        });
    }

    public async Task StopFuzzingAsync()
    {
        _cts?.Cancel();
        await _docker.ExecCommandAsync("afl-multikill");
        if (_monitorTask != null)
            await _monitorTask;
        _monitorTask = null;
        _cts = null;
    }

    private int ParseCrashes(string output)
    {
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Crashes saved"))
            {
                var match = Regex.Match(line, @":\s*(\d+)");
                if (match.Success)
                    return int.Parse(match.Groups[1].Value);
            }
        }
        return 0;
    }
}