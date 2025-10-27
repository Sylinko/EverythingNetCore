// ReSharper disable UnusedMember.Local

using System.Runtime.InteropServices;

namespace EverythingNet.Core;

internal static partial class EverythingWrapper
{
    private static readonly ReaderWriterLockSlim SyncLock = new();

    private class LockWrapper : IDisposable
    {
        private readonly ReaderWriterLockSlim _syncLock;

        public LockWrapper(ReaderWriterLockSlim syncLock)
        {
            _syncLock = syncLock;
            _syncLock.EnterWriteLock();
        }

        public void Dispose()
        {
            _syncLock.ExitWriteLock();
        }
    }

    private const string EverythingDll = "Everything";

    static EverythingWrapper()
    {
        NativeLibrary.SetDllImportResolver(typeof(EverythingWrapper).Assembly, (libraryName, _, _) =>
        {
            if (libraryName != "Everything.dll") return IntPtr.Zero;

            var arch = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "win-x64" : "win-x86";
            var fullPath = Path.Combine(AppContext.BaseDirectory, "runtimes", arch, "native", "Everything.dll");
            return File.Exists(fullPath) ? NativeLibrary.Load(fullPath) : IntPtr.Zero;
        });
    }

    internal static IDisposable Lock()
    {
        return new LockWrapper(SyncLock);
    }

    [LibraryImport(EverythingDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Everything_IsDBLoaded();

    [LibraryImport(EverythingDll)]
    public static partial uint Everything_GetMajorVersion();

    [LibraryImport(EverythingDll)]
    public static partial uint Everything_GetMinorVersion();

    [LibraryImport(EverythingDll)]
    public static partial uint Everything_GetRevision();

    [LibraryImport(EverythingDll)]
    public static partial uint Everything_GetBuildNumber();

    [LibraryImport(EverythingDll, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "Everything_SetSearchW")]
    public static partial void Everything_SetSearch(string lpSearchString);

    [LibraryImport(EverythingDll)]
    public static partial void Everything_SetMatchPath([MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport(EverythingDll)]
    public static partial void Everything_SetMatchCase([MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport(EverythingDll)]
    public static partial void Everything_SetMatchWholeWord([MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [LibraryImport(EverythingDll)]
    public static partial void Everything_SetReplyID(uint nId);

    [LibraryImport(EverythingDll)]
    public static partial void Everything_Reset();

    [LibraryImport(EverythingDll)]
    public static partial int Everything_GetLastError();

    [LibraryImport(EverythingDll, EntryPoint = "Everything_QueryW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Everything_Query([MarshalAs(UnmanagedType.Bool)] bool bWait);

    [LibraryImport(EverythingDll)]
    public static partial uint Everything_GetNumResults();

    [LibraryImport(EverythingDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Everything_IsFileResult(uint nIndex);

    [LibraryImport(EverythingDll, EntryPoint = "Everything_GetResultFullPathNameW")]
    public static unsafe partial void Everything_GetResultFullPathName(uint nIndex, char* buffer, uint nMaxCount);

    [LibraryImport(EverythingDll, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "Everything_GetResultPathW")]
    public static partial string Everything_GetResultPath(uint nIndex);

    [LibraryImport(EverythingDll, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "Everything_GetResultFileNameW")]
    public static partial string Everything_GetResultFileName(uint nIndex);

    // Everything 1.4
    [LibraryImport(EverythingDll)]
    public static partial void Everything_SetSort(uint dwSortType);

    [LibraryImport(EverythingDll)]
    public static partial void Everything_SetRequestFlags(uint dwRequestFlags);

    [LibraryImport(EverythingDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Everything_GetResultSize(uint nIndex, out long lpFileSize);

    [LibraryImport(EverythingDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Everything_GetResultDateCreated(uint nIndex, out long lpFileTime);

    [LibraryImport(EverythingDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Everything_GetResultDateModified(uint nIndex, out long lpFileTime);

    [LibraryImport(EverythingDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Everything_GetResultDateAccessed(uint nIndex, out long lpFileTime);

    [LibraryImport(EverythingDll)]
    public static partial uint Everything_GetResultAttributes(uint nIndex);

    [LibraryImport(EverythingDll)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Everything_GetResultDateRun(uint nIndex, out long lpFileTime);
}