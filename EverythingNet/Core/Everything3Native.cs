using System.Runtime.InteropServices;

namespace EverythingNet.Core;

/// <summary>
/// Declares the win-x64 Everything SDK 3 ABI. Each client owns its connection and result storage.
/// </summary>
internal static partial class Everything3Native
{
    /// <summary>
    /// Calls the native Everything3_ConnectW operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_ConnectW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint ConnectW(string? instanceName);

    /// <summary>
    /// Calls the native Everything3_ShutdownClient operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_ShutdownClient")]
    internal static partial int ShutdownClient(nint client);

    /// <summary>
    /// Calls the native Everything3_DestroyClient operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_DestroyClient")]
    internal static partial int DestroyClient(nint client);

    /// <summary>
    /// Calls the native Everything3_IsDBLoaded operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_IsDBLoaded", SetLastError = true)]
    internal static partial int IsDBLoaded(nint client);

    /// <summary>
    /// Calls the native Everything3_CreateSearchState operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_CreateSearchState", SetLastError = true)]
    internal static partial nint CreateSearchState();

    /// <summary>
    /// Calls the native Everything3_DestroySearchState operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_DestroySearchState")]
    internal static partial int DestroySearchState(nint state);

    /// <summary>
    /// Calls the native Everything3_SetSearchTextW operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_SetSearchTextW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int SetSearchTextW(nint state, string search);

    /// <summary>
    /// Calls the native Everything3_SetSearchViewportCount operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_SetSearchViewportCount")]
    internal static partial int SetSearchViewportCount(nint state, nuint count);

    /// <summary>
    /// Calls the native Everything3_AddSearchPropertyRequest operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_AddSearchPropertyRequest", SetLastError = true)]
    internal static partial int AddSearchPropertyRequest(nint state, uint property);

    /// <summary>
    /// Calls the native Everything3_Search operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_Search", SetLastError = true)]
    internal static partial nint Search(nint client, nint state);

    /// <summary>
    /// Calls the native Everything3_DestroyResultList operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_DestroyResultList")]
    internal static partial int DestroyResultList(nint results);

    /// <summary>
    /// Calls the native Everything3_GetResultListCount operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_GetResultListCount")]
    internal static partial nuint GetResultListCount(nint results);

    /// <summary>
    /// Calls the native Everything3_GetResultListViewportCount operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_GetResultListViewportCount")]
    internal static partial nuint GetResultListViewportCount(nint results);

    /// <summary>
    /// Calls the native Everything3_GetResultFullPathNameW operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_GetResultFullPathNameW")]
    internal static unsafe partial nuint GetResultFullPathNameW(nint results, nuint index, char* buffer, nuint length);

    /// <summary>
    /// Calls the native Everything3_GetResultSize operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_GetResultSize")]
    internal static partial ulong GetResultSize(nint results, nuint index);

    /// <summary>
    /// Calls the native Everything3_GetResultDateCreated operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_GetResultDateCreated")]
    internal static partial ulong GetResultDateCreated(nint results, nuint index);

    /// <summary>
    /// Calls the native Everything3_GetResultDateModified operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_GetResultDateModified")]
    internal static partial ulong GetResultDateModified(nint results, nuint index);

    /// <summary>
    /// Calls the native Everything3_GetResultAttributes operation.
    /// </summary>
    [LibraryImport("Everything3_x64.dll", EntryPoint = "Everything3_GetResultAttributes")]
    internal static partial uint GetResultAttributes(nint results, nuint index);
}