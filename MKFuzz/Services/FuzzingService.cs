using MKFuzz.Models;
using MKFuzz.Services;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MKFuzz.Services;

public class FuzzingService
{
    private readonly DockerService _docker;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    public FuzzingService(DockerService docker)
    {
        _docker = docker;
    }

    public async Task StartFuzzingAsync(FuzzingProject project, IProgress<string> progress, IProgress<FuzzingStats> statsProgress)
    {
        var config = new
        {
            target = project.FuzzBinaryPath,
            cmdline = project.TargetArgs,
            input = "/workspace/seeds",
            output = "/workspace/sync",
            memory = project.MemoryLimit,
            timeout = project.TimeoutMs,
            cores = project.Cores
        };
        var configJson = JsonSerializer.Serialize(config);
        await _docker.ExecCommandAsync($"echo '{configJson}' > /workspace/fuzz.json");

        var fuzzCmd = $"cd /workspace && afl-multicore -c fuzz.json start {project.Cores}";
        _ = _docker.ExecCommandAsync(fuzzCmd);

        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(5000);
                var result = await _docker.ExecCommandAsync("afl-whatsup -s /workspace/sync");
                if (result.ExitCode == 0)
                {
                    var stats = ParseStats(result.Stdout);
                    statsProgress.Report(stats);
                }
            }
        });
    }

    public async Task ResumeFuzzingAsync(FuzzingProject project, IProgress<string> progress, IProgress<FuzzingStats> statsProgress)
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
            memory = project.MemoryLimit,
            timeout = project.TimeoutMs,
            cores = project.Cores
        };
        var configJson = JsonSerializer.Serialize(config);
        await _docker.ExecCommandAsync($"echo '{configJson}' > /workspace/fuzz.json");

        var fuzzCmd = $"cd /workspace && afl-multicore -c fuzz.json resume {project.Cores}";
        _ = _docker.ExecCommandAsync(fuzzCmd);

        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(5000);
                var result = await _docker.ExecCommandAsync("afl-whatsup -s /workspace/sync");
                if (result.ExitCode == 0)
                {
                    var stats = ParseStats(result.Stdout);
                    statsProgress.Report(stats);
                }
            }
        });
    }

    public async Task StopFuzzingAsync()
    {
        _cts?.Cancel();
        await _docker.ExecCommandAsync("afl-multikill");
        if (_monitorTask != null)
            await _monitorTask;
    }

    private FuzzingStats ParseStats(string output)
    {
        var stats = new FuzzingStats();
        var lines = output.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Fuzzers alive"))
            {
                TryParseInt(line, out int val);
                stats.FuzzersAlive = val;
            }
            else if (line.Contains("Total execs"))
            {
                TryParseThousands(line, out long val);
                stats.TotalExecs = val;
            }
            else if (line.Contains("Cumulative speed"))
            {
                TryParseInt(line, out int val);
                stats.ExecsPerSecond = val;
            }
            else if (line.Contains("Crashes saved"))
            {
                TryParseInt(line, out int val);
                stats.Crashes = val;
            }
            else if (line.Contains("Coverage reached"))
            {
                TryParseDouble(line, out double val);
                stats.Coverage = val;
            }
            else if (line.Contains("Pending items"))
            {
                TryParsePending(line, out int val);
                stats.PendingItems = val;
            }
        }
        return stats;
    }

    private void TryParseInt(string line, out int val)
    {
        var match = Regex.Match(line, @":\s*(\d+)");
        val = match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private void TryParseThousands(string line, out long val)
    {
        var match = Regex.Match(line, @":\s*(\d+) thousands");
        val = match.Success ? long.Parse(match.Groups[1].Value) * 1000 : 0;
    }

    private void TryParseDouble(string line, out double val)
    {
        var match = Regex.Match(line, @":\s*([\d.]+)%");
        val = match.Success ? double.Parse(match.Groups[1].Value) : 0;
    }

    private void TryParsePending(string line, out int val)
    {
        var match = Regex.Match(line, @"total\s*(\d+)");
        val = match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }
}