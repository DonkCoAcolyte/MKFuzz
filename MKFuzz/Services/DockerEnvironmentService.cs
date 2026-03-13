using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace MKFuzz.Services;

public class DockerEnvironmentService
{
    private readonly DockerClient _client;

    public DockerEnvironmentService()
    {
        var dockerUri = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "npipe://./pipe/docker_engine"
            : "unix:///var/run/docker.sock";
        _client = new DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
    }

    public async Task<bool> IsDockerRunningAsync()
    {
        try
        {
            await _client.System.PingAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsCorePatternSetCorrectlyAsync()
    {
        var containerConfig = new CreateContainerParameters
        {
            Image = "alpine:latest",
            Cmd = new[] { "cat", "/proc/sys/kernel/core_pattern" },
            AttachStdout = true,
            AttachStderr = true,
            HostConfig = new HostConfig { AutoRemove = true }
        };

        try
        {
            var container = await _client.Containers.CreateContainerAsync(containerConfig);
            await _client.Containers.StartContainerAsync(container.ID, null);

            var logs = await _client.Containers.GetContainerLogsAsync(container.ID,
                new ContainerLogsParameters { ShowStdout = true, ShowStderr = true });

            string output = "";
            using (var reader = new StreamReader(logs))
                output = await reader.ReadToEndAsync();

            var waitResponse = await _client.Containers.WaitContainerAsync(container.ID);
            return output.Trim() == "core";
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SetCorePatternAsync()
    {
        var containerConfig = new CreateContainerParameters
        {
            Image = "alpine:latest",
            Cmd = new[] { "sh", "-c", "echo core > /proc/sys/kernel/core_pattern" },
            HostConfig = new HostConfig
            {
                Privileged = true,
                AutoRemove = true
            }
        };

        try
        {
            var container = await _client.Containers.CreateContainerAsync(containerConfig);
            await _client.Containers.StartContainerAsync(container.ID, null);
            var waitResponse = await _client.Containers.WaitContainerAsync(container.ID);
            return waitResponse.StatusCode == 0;
        }
        catch
        {
            return false;
        }
    }
}