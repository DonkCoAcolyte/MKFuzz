using MKFuzz.Models;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MKFuzz.Services;

public class FuzzingService
{
    private const int AFL_FUZZER_STATS_UPDATE_INTERVAL = 60 * 1000;

    private readonly DockerService _docker;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    public FuzzingService(DockerService docker)
    {
        _docker = docker;
    }

    public async Task StartFuzzingAsync(FuzzingProject project, IProgress<string> progress, IProgress<string> rawStats)
    {
        var config = new
        {
            target = project.FuzzBinaryPath,
            cmdline = project.TargetArgs,
            input = "/workspace/seeds",
            output = "/workspace/sync",
            memory = project.MemoryLimit.ToString(),
            timeout = project.TimeoutMs.ToString()
        };
        var configJson = JsonSerializer.Serialize(config);
        await _docker.ExecCommandAsync($"echo '{configJson}' > /workspace/fuzz.json");

        var fuzzCmd = $"cd /workspace && afl-multicore -c fuzz.json start {project.Cores}";
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

    public async Task ResumeFuzzingAsync(FuzzingProject project, IProgress<string> progress, IProgress<string> rawStats)
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
            memory = project.MemoryLimit.ToString(),
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
}