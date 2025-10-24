using static EverythingNet.Core.EverythingWrapper;

namespace EverythingNet.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Interfaces;

public sealed class Everything : IEverything, IDisposable
{
    private static uint lastReplyId;

    private readonly uint replyId;

    private const uint DefaultSearchFlags = (uint)(
        RequestFlags.EVERYTHING_REQUEST_SIZE
        | RequestFlags.EVERYTHING_REQUEST_FILE_NAME
        | RequestFlags.EVERYTHING_REQUEST_EXTENSION
        | RequestFlags.EVERYTHING_REQUEST_PATH
        | RequestFlags.EVERYTHING_REQUEST_FULL_PATH_AND_FILE_NAME
        | RequestFlags.EVERYTHING_REQUEST_DATE_MODIFIED);

    public Everything()
    {
        ResultKind = ResultKind.Both;
        replyId = Interlocked.Increment(ref lastReplyId);
        if (!EverythingState.IsStarted())
        {
            throw new InvalidOperationException("Everything service must be started");
        }
    }

    public ResultKind ResultKind { get; set; }

    public bool MatchCase { get; set; }

    public bool MatchPath { get; set; }

    public bool MatchWholeWord { get; set; }

    public SortingKey SortKey { get; set; }

    public ErrorCode LastErrorCode { get; set; }

    public long Count => Everything_GetNumResults();

    public void Reset()
    {
        Everything_Reset();
    }

    public void Dispose()
    {
        Reset();
    }

    public IEnumerable<ISearchResult> SendSearch(string searchPattern, RequestFlags flags)
    {
        ArgumentNullException.ThrowIfNull(searchPattern);
        
        if (string.IsNullOrWhiteSpace(searchPattern))
        {
            return [];
        }

        using (Lock())
        {
            Everything_SetReplyID(replyId);
            Everything_SetMatchWholeWord(MatchWholeWord);
            Everything_SetMatchPath(MatchPath);
            Everything_SetMatchCase(MatchCase);
            Everything_SetRequestFlags((uint)flags | DefaultSearchFlags);
            searchPattern = ApplySearchResultKind(searchPattern);
            Everything_SetSearch(searchPattern);

            if (SortKey != SortingKey.None)
            {
                Everything_SetSort((uint)SortKey);
            }

            Everything_Query(true);
            LastErrorCode = GetError();
            return GetResults();
        }
    }

    private string ApplySearchResultKind(string searchPattern) => ResultKind switch
    {
        ResultKind.FilesOnly => $"files: {searchPattern}",
        ResultKind.FoldersOnly => $"folders: {searchPattern}",
        _ => searchPattern
    };

    private IEnumerable<ISearchResult> GetResults()
    {
        var numResults = Everything_GetNumResults();
        return Enumerable.Range(0, (int)numResults).Select(x => new SearchResult(x, replyId));
    }

    private static ErrorCode GetError()
    {
        var error = Everything_GetLastError();
        return (ErrorCode)error;
    }
}