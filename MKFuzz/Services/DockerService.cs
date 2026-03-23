using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace MKFuzz.Services;

public class DockerService : IAsyncDisposable
{
    private readonly DockerClient _client;
    private string? _containerId;

    public DockerService()
    {
        var dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "npipe://./pipe/docker_engine"
            : "unix:///var/run/docker.sock";
        _client = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
    }

    public async Task StartContainerAsync(string imageName, Dictionary<string, string> volumeMounts)
    {
        var binds = volumeMounts.Select(kv => $"{kv.Key}:{kv.Value}").ToList();

        var hostConfig = new HostConfig
        {
            Binds = binds,
            // Privileged = false (removed)
            Tmpfs = new Dictionary<string, string> { { "/tmp", "size=2G" } }
        };

        var createParams = new CreateContainerParameters
        {
            Image = imageName,
            HostConfig = hostConfig,
            Tty = true,
            OpenStdin = true,
            StdinOnce = false,
            Cmd = new List<string> { "/bin/bash", "-c", "sleep infinity" }
        };

        var response = await _client.Containers.CreateContainerAsync(createParams);
        _containerId = response.ID;
        await _client.Containers.StartContainerAsync(_containerId, null);
    }

    public async Task<(int ExitCode, string Stdout, string Stderr)> ExecCommandAsync(string command)
    {
        if (_containerId == null)
            throw new InvalidOperationException("Container not started.");

        var execCreateParams = new ContainerExecCreateParameters
        {
            AttachStdout = true,
            AttachStderr = true,
            Cmd = new[] { "/bin/bash", "-c", command }
        };

        var execCreateResponse = await _client.Exec.ExecCreateContainerAsync(_containerId, execCreateParams);

        using var stream = await _client.Exec.StartAndAttachContainerExecAsync(execCreateResponse.ID, false);

        var output = await stream.ReadOutputToEndAsync(default);
        string stdout = output.stdout;
        string stderr = output.stderr;

        var inspectResponse = await _client.Exec.InspectContainerExecAsync(execCreateResponse.ID);
        int exitCode = (int)inspectResponse.ExitCode;
        return (exitCode, stdout, stderr);
    }

    public async Task StopContainerAsync()
    {
        if (_containerId == null) return;
        await _client.Containers.StopContainerAsync(_containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 });
        await _client.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters { Force = true });
        _containerId = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_containerId != null)
            await StopContainerAsync();
        _client.Dispose();
    }
}