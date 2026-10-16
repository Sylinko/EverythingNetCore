using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using EverythingNet;

/// <summary>
/// Exercises the packaged client against an isolated, folder-only Everything instance.
/// No installed process, service, or user index is changed.
/// </summary>
internal static class ClientSmoke
{
    /// <summary>
    /// Checks native result ownership, independent clients, cancellation, and disconnection.
    /// </summary>
    internal static async Task RunAsync(string packagedExecutable)
    {
        var directory = Path.Combine(Path.GetTempPath(), "EverythingNetCore-Smoke-" + Guid.NewGuid().ToString("N"));
        var files = Path.Combine(directory, "files");
        Directory.CreateDirectory(files);
        for (var index = 0; index < 3; index++)
        {
            await File.WriteAllTextAsync(Path.Combine(files, $"smoke-文件-{index}.txt"), "probe");
        }

        var executable = Path.Combine(directory, "Everything.exe");
        File.Copy(packagedExecutable, executable);
        var configuration = Path.Combine(directory, "Everything.ini");
        await File.WriteAllTextAsync(configuration,
            "[Everything]\r\napp_data=0\r\nrun_as_admin=0\r\nindex_as_admin=0\r\nshow_tray_icon=0\r\ncheck_for_updates_on_startup=0\r\n");
        var instanceName = "EverythingNetCore-Smoke-" + Guid.NewGuid().ToString("N");
        using var process = Start(executable, "-instance", instanceName, "-startup", "-config", configuration,
            "-db", Path.Combine(directory, "Everything.db"), "-no-auto-include", "-no-auto-index", "-folders", files, "-is-run-as");

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var client = await ConnectAsync(instanceName, timeout.Token);
            while (!client.IsDatabaseLoaded(timeout.Token))
            {
                await Task.Delay(100, timeout.Token);
            }

            for (var round = 0; round < 3; round++)
            {
                var other = await ConnectAsync(instanceName, timeout.Token);
                EverythingSearchResponse snapshot;
                using (other)
                {
                    snapshot = other.Search("smoke-文件-", 2, timeout.Token);
                    if (!other.IsUsingSdk3 || snapshot.TotalCount != 3 || snapshot.Results.Count != 2 ||
                        snapshot.Results.Any(result => !File.Exists(result.FullPath) || !result.IsFile || result.Size != 5))
                    {
                        throw new InvalidOperationException("The SDK 3 query returned incorrect copied results.");
                    }
                }

                if (client.Search("smoke-文件-2.txt", 1, timeout.Token).TotalCount != 1 ||
                    snapshot.Results.Any(result => !result.FullPath.Contains("smoke-文件-")))
                {
                    throw new InvalidOperationException("Disposing another client affected the connection or result snapshot.");
                }
            }

            await CheckCancellationAsync();
            if (client.Search("smoke-文件-", 1, timeout.Token).TotalCount != 3)
            {
                throw new InvalidOperationException("Canceling a different connection affected the active client.");
            }

            using var stop = Start(executable, "-instance", instanceName, "-exit");
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                client.IsDatabaseLoaded();
                throw new InvalidOperationException("A disconnected server was reported as an index still loading.");
            }
            catch (Win32Exception)
            {
                Console.WriteLine("Client smoke passed: SDK 3 queries, snapshots, independent disposal, cancellation, and disconnection.");
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
            }

            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Waits for this test's process to expose its IPC endpoint.
    /// </summary>
    private static async Task<EverythingClient> ConnectAsync(string instanceName, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var client = EverythingClient.TryConnect(instanceName);
            if (client is not null)
            {
                return client;
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    /// <summary>
    /// A pipe that accepts a request but withholds its response reproduces a blocked native read.
    /// Cancellation must join that read before the client is destroyed.
    /// </summary>
    private static async Task CheckCancellationAsync()
    {
        var instanceName = "EverythingNetCore-Stalled-" + Guid.NewGuid().ToString("N");
        await using var pipe = new NamedPipeServerStream("Everything IPC (" + instanceName + ")",
            PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        var connection = pipe.WaitForConnectionAsync();
        using var client = EverythingClient.TryConnect(instanceName)
            ?? throw new InvalidOperationException("The isolated test pipe could not be connected.");
        await connection.WaitAsync(TimeSpan.FromSeconds(5));

        using var cancellation = new CancellationTokenSource();
        var search = Task.Run(() => client.Search("smoke", 1, cancellation.Token));
        try
        {
            var buffer = new byte[1024];
            await pipe.ReadAsync(buffer).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            cancellation.Cancel();
        }

        try
        {
            await search.WaitAsync(TimeSpan.FromSeconds(5));
            throw new InvalidOperationException("The active native query ignored cancellation.");
        }
        catch (OperationCanceledException)
        {
            // Cancellation is reported only after the active native call has returned.
        }
    }

    /// <summary>
    /// Starts only the test-owned executable and instance, with no visible window.
    /// </summary>
    private static Process Start(string executable, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetDirectoryName(executable)
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException("The isolated Everything instance could not start.");
    }
}
