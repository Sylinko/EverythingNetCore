[![.NET](https://github.com/2A5F/EverythingNetCore/actions/workflows/dotnet.yml/badge.svg)](https://github.com/2A5F/EverythingNetCore/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/EverythingNetCore.svg?style=flat)](https://www.nuget.org/packages/EverythingNetCore/)

# EverythingNet ![](media/site_logo.gif)

> ## ***Original repo: https://github.com/ju2pom/EverythingNet***

## Check demo app

https://github.com/ju2pom/EverythingNetDemo

## What

EverythingNet is a C# library that wraps the great library from voidtools named Everything. This library lets you search for files and folders incredibly fast. For more information jump to the [official page](https://www.voidtools.com/)

EverythingNet provides a simple .NET API that wraps aforementioned library (which is coded in C). It doesn't rely on Windows Search at all but on a running Everything application and its index. The indexing service alone does not accept SDK queries.

EverythingNet exposes a fluent API that ease access to specific search functions.

## Instance-aware client (Windows x64)

`EverythingClient` connects to an existing Everything process without starting or stopping it.
It first tries the Everything 1.5 SDK 3 endpoint. An unnamed connection can also use
Everything 1.4's default instance through SDK 2. For a named 1.5 instance, pass its name
(for example `"1.5a"`); custom names are supported. The client and packaged native assets
now require a Windows x64 process. There is no x86 client distribution.

```csharp
using EverythingNet;

using var client = EverythingClient.TryConnect() ?? EverythingClient.TryConnect("1.5a")
    ?? throw new InvalidOperationException("Start Everything first.");

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
while (!client.IsDatabaseLoaded(timeout.Token))
{
    await Task.Delay(200, timeout.Token);
}

var response = client.Search("ext:txt", maxResults: 100, timeout.Token);
foreach (var result in response.Results)
{
    Console.WriteLine(result.FullPath);
}
```

The readiness and search calls are synchronous native operations. Run them on a worker
thread in a UI application. Results are copied values that remain usable after the client
is disposed. The new client does not expose SDK-global lazy result indexes.

SDK 3 cancellation shuts down that client's connection and joins the active call; dispose
and reconnect before another query. SDK 2 cannot abort an active synchronous IPC query:
cancellation is observed after it returns, and disposal waits rather than freeing state
under the call. SDK 2 operations are serialized with the legacy wrapper's global lock.
Do not mix the legacy API's lazy results with concurrent searches or cleanup.

The caller owns process policy: reuse or start a user installation, or explicitly create
and later stop its own named instance. Disposing `EverythingClient` only releases client
state; it never calls the process-wide `Everything_Exit` API. The older `EverythingState`
process-control API remains available for existing callers and has different ownership
semantics; a plugin should use the new client.

The package carries `runtimes/win-x64/native/Everything.dll` (official SDK 2 x64, refreshed 2026-10-16),
`Everything3_x64.dll` (SDK 3.0.0.9), and `Everything.exe` (1.5.0.1424b).
RID-specific publishing may flatten these assets. A collectible plugin's load context
owns native resolution; the library's standalone fallback does not preempt that context.
The application owns native callbacks, active calls and library lifetime.

Binary sources: [Everything 1.5 downloads](https://www.voidtools.com/forum/viewtopic.php?f=12&t=9787),
[SDK 2](https://www.voidtools.com/Everything-SDK.zip),
[SDK 3.0.0.9](https://github.com/voidtools/everything_sdk3/releases/tag/3.0.0.9).
See `LICENSE` for Everything, SDK and library notices. SDK 3.0.0.9's viewport-count setter
updates valid state but returns FALSE; this wrapper ignores that incorrect return value.

### Compatibility and lifetime

- Supported scope: Everything 1.4.1 default instance and Everything 1.5 default/named instances,
  from a Windows x64 process. Versions before 1.4 are outside this client's support scope.
- `TryConnect(name)` addresses exactly that instance. It does not discover installations,
  launch processes, install services, retry indefinitely in managed code, or select another name.
  The native SDK may wait when a pipe is busy; connection establishment has no cancellation token.
- `IsUsingSdk3` identifies the connection backend, not the Everything application version.
  Officially, SDK 2 is also compatible with 1.5.
- `IsDatabaseLoaded()` returns false while the index loads and throws on IPC failure.
  Search itself waits for the index to load, so readiness polling is optional.
- `Search` and `Dispose` serialize access to the same client. SDK 3 cancellation uses the
  official `ShutdownClient` operation, which invalidates the connection. Reconnect explicitly.
  SDK 2 cancellation is cooperative only before/after the native call; a timeout token is
  not a hard time limit. This wrapper does not add a hidden window or message loop to change that.
- A result snapshot is independent of the native client and survives later searches/disposal.
  Disposing a client does not affect the user's Everything process or another SDK 3 client.

The official [SDK 3 guide](https://github.com/voidtools/everything_sdk3) and
[API reference](https://www.voidtools.com/forum/viewtopic.php?t=15853) describe instance
selection and client/search-state/result-list ownership. The [SDK 2 reference](https://www.voidtools.com/support/everything/sdk/)
documents the legacy query pipeline. The wrapper follows those lifetimes rather than
managing the Everything installation. SDK 3.0.0.9 was verified against 1.5.0.1423b and
1.5.0.1424b. SDK 2 was verified against 1.4.1.1028; the upstream main branch is not substituted for a released native SDK.

## Features

The fluent API provides the following set of features:
- [x] thread safety
- [x] search for files only or folders only or both
- [x] name contains/start with/end with
- [x] search by extension or list of extensions
- [x] logical operators (Not, And, Or)
- [x] size search criteria
- [x] picture properties search criteria (format, dimensions)
- [x] audio search criteria (ID3 Tags)
- [x] dates search criteria (creation, modification, access, execution)
- [ ] file content (maybe later)

## How

The library exposes a fluent API that ease access to specific search functions.
Here is a very simple example:

```csharp
IEverything everything = new Everything();
var results = everything.Search().Name.Contains("temp");
```

## Wiki
[Wiki](https://github.com/ju2pom/EverythingNet/wiki)
