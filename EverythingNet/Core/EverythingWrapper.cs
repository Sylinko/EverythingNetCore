// ReSharper disable UnusedMember.Local
namespace EverythingNet.Core;

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static partial class EverythingWrapper
{
    private static readonly ReaderWriterLockSlim SyncLock = new();

    private class Locker : IDisposable
    {
        private readonly ReaderWriterLockSlim _syncLock;

        public Locker(ReaderWriterLockSlim syncLock)
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

    private const int EVERYTHING_OK = 0;
    private const int EVERYTHING_ERROR_MEMORY = 1;
    private const int EVERYTHING_ERROR_IPC = 2;
    private const int EVERYTHING_ERROR_REGISTERCLASSEX = 3;
    private const int EVERYTHING_ERROR_CREATEWINDOW = 4;
    private const int EVERYTHING_ERROR_CREATETHREAD = 5;
    private const int EVERYTHING_ERROR_INVALIDINDEX = 6;
    private const int EVERYTHING_ERROR_INVALIDCALL = 7;

    public enum FileInfoIndex
    {
        FileSize = 1,
        FolderSize,
        DateCreated,
        DateModified,
        DateAccessed,
        Attributes
    }


    internal static IDisposable Lock()
    {
        return new Locker(SyncLock);
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

    [DllImport(EverythingDll, CharSet = CharSet.Unicode, EntryPoint = "Everything_GetResultFullPathNameW")]
    public static extern void Everything_GetResultFullPathName(uint nIndex, StringBuilder lpString, uint nMaxCount);

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