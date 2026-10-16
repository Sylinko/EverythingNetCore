namespace EverythingNet;

/// <summary>
/// Owns copied result values independently of native SDK state and later searches.
/// </summary>
public sealed record EverythingSearchResult(
    string FullPath,
    bool IsFile,
    long Size,
    DateTime? Created,
    DateTime? Modified,
    FileAttributes Attributes);

/// <summary>
/// Contains total matches and the bounded result viewport requested by the caller.
/// </summary>
public sealed record EverythingSearchResponse(ulong TotalCount, IReadOnlyList<EverythingSearchResult> Results);