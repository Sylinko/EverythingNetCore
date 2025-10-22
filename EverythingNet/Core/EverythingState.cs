namespace EverythingNet.Core;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Interfaces;

public static class EverythingState
{
    public enum StartMode
    {
        Install,
        Service
    }

    public static Process? Process { get; private set; }

    public static bool IsStarted()
    {
        var version = GetVersion();
        return version.Major > 0;
    }

    public static void StartService(bool admin, StartMode mode)
    {
        if (!IsStarted())
        {
            var option = admin ? "-admin" : string.Empty;
            switch (mode)
            {
                case StartMode.Install:
                    option += " -install-service";
                    break;
                case StartMode.Service:
                    option += " -startup";
                    break;
            }
            StartProcess(option);
            IsStarted();
        }
    }

    public static bool IsReady()
    {
        return EverythingWrapper.Everything_IsDBLoaded();
    }

    private static Version GetVersion()
    {
        var major = EverythingWrapper.Everything_GetMajorVersion();
        var minor = EverythingWrapper.Everything_GetMinorVersion();
        var build = EverythingWrapper.Everything_GetBuildNumber();
        var revision = EverythingWrapper.Everything_GetRevision();

        return new Version(Convert.ToInt32(major), Convert.ToInt32(minor), Convert.ToInt32(build), Convert.ToInt32(revision));
    }

    public static ErrorCode GetLastError()
    {
        return (ErrorCode)EverythingWrapper.Everything_GetLastError();
    }

    private static void StartProcess(string options)
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var exePath = Path.GetFullPath(
            Path.Combine(path, "runtimes", Environment.Is64BitProcess ? "win-x64" : "win-x86", "native", "Everything.exe"));

        if (!File.Exists(exePath))
        {
            throw new Exception("Everything.exe not found");
        }

        Process = Process.Start(exePath, options);
    }
}