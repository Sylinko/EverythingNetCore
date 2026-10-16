using EverythingNet;
using EverythingNet.Core;

if (!OperatingSystem.IsWindows())
{
    throw new PlatformNotSupportedException("Everything package consumption must be checked on Windows.");
}

var rid = "win-x64";
foreach (var file in new[] { "Everything.dll", "Everything3_x64.dll", "Everything.exe" })
{
    var rootPath = Path.Combine(AppContext.BaseDirectory, file);
    var runtimePath = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", file);
    if (!File.Exists(rootPath) && !File.Exists(runtimePath))
    {
        throw new FileNotFoundException($"The package did not distribute {file} for {rid}.");
    }
}

// Invoke the packaged DLL without starting a service or changing the machine.
var isStarted = EverythingState.IsStarted();
Console.WriteLine($"Everything package smoke passed: {rid}, existing default instance reachable={isStarted}.");

// Exercise both native imports even on a clean CI machine with no Everything application running.
using var missing = EverythingClient.TryConnect("EverythingNetCore-Missing-" + Guid.NewGuid().ToString("N"));
if (missing is not null)
{
    throw new InvalidOperationException("A connection unexpectedly resolved to a different instance.");
}

if (args.Contains("--exercise-client"))
{
    var executable = Path.Combine(AppContext.BaseDirectory, "Everything.exe");
    if (!File.Exists(executable))
    {
        executable = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", "Everything.exe");
    }

    await ClientSmoke.RunAsync(executable);
}
