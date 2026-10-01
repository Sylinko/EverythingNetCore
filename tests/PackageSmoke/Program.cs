using EverythingNet.Core;

if (!OperatingSystem.IsWindows())
{
    throw new PlatformNotSupportedException("Everything package consumption must be checked on Windows.");
}

var rid = Environment.Is64BitProcess ? "win-x64" : "win-x86";
foreach (var file in new[] { "Everything.dll", "Everything.exe" })
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
Console.WriteLine($"Everything package smoke passed: {rid}, existing service running={isStarted}.");
