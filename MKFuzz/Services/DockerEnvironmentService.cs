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
            HostConfig = new HostConfig
            {
                AutoRemove = false // important!
            }
        };

        try
        {
            var container = await _client.Containers.CreateContainerAsync(containerConfig);
            await _client.Containers.StartContainerAsync(container.ID, null);

            // Wait for the container to finish
            var waitResponse = await _client.Containers.WaitContainerAsync(container.ID);
            if (waitResponse.StatusCode != 0)
            {
                // Command failed – clean up and return false
                await _client.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
                return false;
            }

            // Now get logs – use the non‑obsolete overload with timestamps = false
            var logs = await _client.Containers.GetContainerLogsAsync(
                container.ID,
                false,
                parameters: new ContainerLogsParameters { ShowStdout = true, ShowStderr = true },
                cancellationToken: default);

            // Read the multiplexed stream to separate stdout/stderr strings
            var (stdout, stderr) = await logs.ReadOutputToEndAsync(default);

            // Clean up the container
            await _client.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });

            return stdout.Trim() == "core";
        }
        catch (Exception ex)
        {
            // Log if desired
            Console.WriteLine($"Error in IsCorePatternSetCorrectlyAsync: {ex}");
            return false;
        }
    }

    public async Task<bool> SetCorePatternAsync()
    {
        var containerConfig = new CreateContainerParameters
        {
            Image = "alpine:latest",
            Cmd = new[] { "sh", "-c", "echo core > /proc/sys/kernel/core_pattern" },
            AttachStdout = true,   // So we can capture output if needed
            AttachStderr = true,   // Crucial to see error messages
            HostConfig = new HostConfig
            {
                Privileged = true,
                AutoRemove = false   // Disable auto-remove so we can read logs after exit
            }
        };

        try
        {
            // Create and start the container
            var container = await _client.Containers.CreateContainerAsync(containerConfig);
            await _client.Containers.StartContainerAsync(container.ID, null);

            // Wait for the command to finish
            var waitResponse = await _client.Containers.WaitContainerAsync(container.ID);

            // Now fetch the logs (both stdout and stderr)
            var logs = await _client.Containers.GetContainerLogsAsync(
                container.ID,
                false,
                parameters: new ContainerLogsParameters { ShowStdout = true, ShowStderr = true },
                cancellationToken: default);

            var (stdout, stderr) = await logs.ReadOutputToEndAsync(default);

            // Clean up the container (we are done with it)
            await _client.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });

            // If exit code is non‑zero, log the error and return false
            if (waitResponse.StatusCode != 0)
            {
                Console.WriteLine($"SetCorePatternAsync failed with exit code {waitResponse.StatusCode}. stderr: {stderr}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in SetCorePatternAsync: {ex}");
            return false;
        }
    }
}