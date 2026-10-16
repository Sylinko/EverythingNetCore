using System.ComponentModel;
using System.Runtime.InteropServices;
using EverythingNet.Core;
using EverythingNet.Interfaces;

namespace EverythingNet;

/// <summary>
/// Connects to an existing Everything 1.4 default instance or an Everything 1.5 instance.
/// This type never starts, installs, stops or otherwise takes ownership of the Everything application.
/// Searches return copied values. Dispose waits for any synchronous native operation to finish.
/// </summary>
public sealed class EverythingClient : IDisposable
{
    /// <summary>
    /// Gets whether this connection uses SDK 3. SDK 2 can also connect to an Everything 1.5 default instance.
    /// </summary>
    public bool IsUsingSdk3 => _client != nint.Zero;

    private readonly Lock _gate = new();
    private readonly nint _client;
    private bool _isDisposed;

    /// <summary>
    /// Represents an established connection; zero identifies the SDK 2 default-instance backend.
    /// </summary>
    private EverythingClient(nint client)
    {
        _client = client;
    }

    /// <summary>
    /// Tries SDK 3 first, then SDK 2 only for the unnamed instance. Connection failure returns null.
    /// The caller can probe the historical 1.5a name or a configured named instance separately.
    /// </summary>
    public static EverythingClient? TryConnect(string? instanceName = null)
    {
        EverythingWrapper.EnsureInitialized();
        var client = Everything3Native.ConnectW(instanceName);
        if (client != nint.Zero)
        {
            return new EverythingClient(client);
        }

        if (!string.IsNullOrEmpty(instanceName))
        {
            return null;
        }

        using var scope = EverythingWrapper.Lock();
        return EverythingWrapper.Everything_GetMajorVersion() == 1 && EverythingWrapper.Everything_GetMinorVersion() >= 4
            ? new EverythingClient(nint.Zero)
            : null;
    }

    /// <summary>
    /// Checks database readiness. SDK 3 cancellation shuts down this connection; reconnect before reuse.
    /// Returns false only when the database is not ready; connection errors are reported as exceptions.
    /// SDK 2 retains ownership until its synchronous readiness call returns.
    /// </summary>
    public bool IsDatabaseLoaded(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            if (IsUsingSdk3)
            {
                using var registration = cancellationToken.Register(() => Everything3Native.ShutdownClient(_client));
                var isLoaded = Everything3Native.IsDBLoaded(_client) != 0;
                var error = Marshal.GetLastPInvokeError();
                cancellationToken.ThrowIfCancellationRequested();
                if (!isLoaded && error != 0)
                {
                    throw new Win32Exception(error);
                }

                return isLoaded;
            }

            using var scope = EverythingWrapper.Lock();
            var isDatabaseLoaded = EverythingWrapper.Everything_IsDBLoaded();
            var sdk2Error = EverythingWrapper.Everything_GetLastError();
            cancellationToken.ThrowIfCancellationRequested();
            if (!isDatabaseLoaded && sdk2Error != 0)
            {
                throw new InvalidOperationException($"Everything SDK 2 readiness check failed with error {sdk2Error}.");
            }

            return isDatabaseLoaded;
        }
    }

    /// <summary>
    /// Executes a serialized search and copies all requested fields before releasing native state.
    /// SDK 3 cancellation shuts down this connection; dispose it and reconnect before another search.
    /// SDK 2 cannot abort a synchronous query: cancellation is observed after native execution returns.
    /// </summary>
    public EverythingSearchResponse Search(string search, int maxResults, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            return IsUsingSdk3
                ? SearchSdk3(search, maxResults, cancellationToken)
                : SearchSdk2(search, maxResults, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (IsUsingSdk3)
            {
                Everything3Native.DestroyClient(_client);
            }
            else
            {
                using var scope = EverythingWrapper.Lock();
                EverythingWrapper.Everything_CleanUp();
            }
        }
    }

    /// <summary>
    /// Uses an independent result list; the cancellation callback is joined before native handles are destroyed.
    /// </summary>
    private unsafe EverythingSearchResponse SearchSdk3(string search, int maxResults, CancellationToken cancellationToken)
    {
        using var registration = cancellationToken.Register(() => Everything3Native.ShutdownClient(_client));
        var state = Everything3Native.CreateSearchState();
        if (state == nint.Zero)
        {
            throw NativeError();
        }

        var results = nint.Zero;

        try
        {
            Check(Everything3Native.SetSearchTextW(state, search));
            // SDK 3.0.0.9 assigns the count to this valid state but always returns FALSE.
            Everything3Native.SetSearchViewportCount(state, (nuint)maxResults);

            // SDK 3 property IDs: full path (240), size (2), modified (5), created (6), attributes (8).
            foreach (var property in new uint[] { 240, 2, 5, 6, 8 })
            {
                Check(Everything3Native.AddSearchPropertyRequest(state, property));
            }

            results = Everything3Native.Search(_client, state);
            var error = Marshal.GetLastPInvokeError();
            cancellationToken.ThrowIfCancellationRequested();
            if (results == nint.Zero)
            {
                throw new Win32Exception(error);
            }

            var count = Everything3Native.GetResultListViewportCount(results);
            var records = new List<EverythingSearchResult>((int)count);

            for (var index = (nuint)0; index < count; index++)
            {
                var length = Everything3Native.GetResultFullPathNameW(results, index, null, 0);
                var buffer = new char[checked((int)length + 1)];
                fixed (char* pointer = buffer)
                {
                    Everything3Native.GetResultFullPathNameW(results, index, pointer, (nuint)buffer.Length);
                }

                var attributes = Everything3Native.GetResultAttributes(results, index);
                var isFile = (attributes & (uint)FileAttributes.Directory) == 0;
                var size = Everything3Native.GetResultSize(results, index);
                records.Add(new EverythingSearchResult(
                    new string(buffer.AsSpan(0, (int)length).TrimEnd('\0')), isFile,
                    isFile && size <= long.MaxValue ? (long)size : -1,
                    GetDate(Everything3Native.GetResultDateCreated(results, index)),
                    GetDate(Everything3Native.GetResultDateModified(results, index)),
                    attributes == uint.MaxValue ? FileAttributes.None : (FileAttributes)attributes));
            }

            return new EverythingSearchResponse(Everything3Native.GetResultListCount(results), records);
        }
        finally
        {
            if (results != nint.Zero)
            {
                Everything3Native.DestroyResultList(results);
            }

            Everything3Native.DestroySearchState(state);
        }
    }

    /// <summary>
    /// SDK 2 shares process-global search state, so query, error checks and all result copies share its lock.
    /// No lazy SearchResult or native index escapes the operation.
    /// </summary>
    private static unsafe EverythingSearchResponse SearchSdk2(string search, int maxResults, CancellationToken cancellationToken)
    {
        using var scope = EverythingWrapper.Lock();
        cancellationToken.ThrowIfCancellationRequested();
        EverythingWrapper.Everything_Reset();
        EverythingWrapper.Everything_SetSearch(search);
        EverythingWrapper.Everything_SetMax((uint)maxResults);
        EverythingWrapper.Everything_SetRequestFlags((uint)(
            RequestFlags.EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME | RequestFlags.EVERYTHING_REQUEST_SIZE |
            RequestFlags.EVERYTHING_REQUEST_DATE_CREATED | RequestFlags.EVERYTHING_REQUEST_DATE_MODIFIED |
            RequestFlags.EVERYTHING_REQUEST_ATTRIBUTES));

        if (!EverythingWrapper.Everything_Query(true))
        {
            throw new InvalidOperationException($"Everything SDK 2 query failed with error {EverythingWrapper.Everything_GetLastError()}.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var count = EverythingWrapper.Everything_GetNumResults();
        var records = new List<EverythingSearchResult>((int)count);

        for (var index = 0u; index < count; index++)
        {
            var length = EverythingWrapper.Everything_GetResultFullPathName(index, null, 0);
            var buffer = new char[checked((int)length + 1)];
            fixed (char* pointer = buffer)
            {
                EverythingWrapper.Everything_GetResultFullPathName(index, pointer, (uint)buffer.Length);
            }

            var isFile = EverythingWrapper.Everything_IsFileResult(index);
            var hasSize = EverythingWrapper.Everything_GetResultSize(index, out var size);
            var hasCreated = EverythingWrapper.Everything_GetResultDateCreated(index, out var created);
            var hasModified = EverythingWrapper.Everything_GetResultDateModified(index, out var modified);
            var attributes = EverythingWrapper.Everything_GetResultAttributes(index);
            records.Add(new EverythingSearchResult(
                new string(buffer.AsSpan(0, (int)length).TrimEnd('\0')), isFile, isFile && hasSize ? size : -1,
                hasCreated ? GetDate((ulong)created) : null,
                hasModified ? GetDate((ulong)modified) : null,
                attributes == uint.MaxValue ? FileAttributes.None : (FileAttributes)attributes));
        }

        return new EverythingSearchResponse(EverythingWrapper.Everything_GetTotResults(), records);
    }

    /// <summary>
    /// Treats the SDK's unknown date sentinel as absent instead of reading the filesystem synchronously.
    /// </summary>
    private static DateTime? GetDate(ulong fileTime)
    {
        return fileTime <= 2650467743999999999 ? DateTime.FromFileTimeUtc((long)fileTime) : null;
    }

    /// <summary>
    /// Reads the SDK 3 error captured by the P/Invoke stub before managed execution resumes.
    /// </summary>
    private static Win32Exception NativeError() => new(Marshal.GetLastPInvokeError());

    /// <summary>
    /// Checks native search configuration failures before executing a query.
    /// </summary>
    private static void Check(int result)
    {
        if (result == 0)
        {
            throw NativeError();
        }
    }
}