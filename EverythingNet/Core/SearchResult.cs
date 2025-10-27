using System.Diagnostics.CodeAnalysis;
using EverythingNet.Interfaces;

namespace EverythingNet.Core;

internal class SearchResult(uint index, uint replyId) : ISearchResult
{
    private delegate bool MyDelegate(uint index, out long date);

    private readonly uint _index = Convert.ToUInt32(index);

    public long Index => _index;

    public bool IsFile => EverythingWrapper.Everything_IsFileResult(_index);

    [field: AllowNull, MaybeNull]
    public unsafe string FullPath
    {
        get
        {
            if (field != null) return field;

            var buffer = stackalloc char[520];

            EverythingWrapper.Everything_SetReplyID(replyId);
            EverythingWrapper.Everything_GetResultFullPathName(_index, buffer, 520);

            field = new string(buffer);

            return field;
        }
    }

    public string Path
    {
        get
        {
            EverythingWrapper.Everything_SetReplyID(replyId);
            return EverythingWrapper.Everything_GetResultPath(index);
        }
    }

    public string FileName
    {
        get
        {
            EverythingWrapper.Everything_SetReplyID(replyId);
            return EverythingWrapper.Everything_GetResultFileName(index);
        }
    }

    public long Size
    {
        get
        {
            EverythingWrapper.Everything_SetReplyID(replyId);
            EverythingWrapper.Everything_GetResultSize(_index, out var size);

            return size;
        }
    }

    public uint Attributes
    {
        get
        {
            EverythingWrapper.Everything_SetReplyID(replyId);
            var attributes = EverythingWrapper.Everything_GetResultAttributes(_index);

            return attributes > 0 ? attributes
                : !string.IsNullOrEmpty(FullPath) ? (uint)File.GetAttributes(FullPath)
                : 0;
        }
    }

    public DateTime Created => GenericDate(EverythingWrapper.Everything_GetResultDateCreated, File.GetCreationTime);

    public DateTime Modified => GenericDate(EverythingWrapper.Everything_GetResultDateModified, File.GetLastWriteTime);

    public DateTime Accessed => GenericDate(EverythingWrapper.Everything_GetResultDateAccessed, File.GetLastAccessTime);

    public DateTime Executed => GenericDate(EverythingWrapper.Everything_GetResultDateRun, File.GetLastAccessTime);

    private DateTime GenericDate(MyDelegate func, Func<string, DateTime> fallbackDelegate)
    {
        EverythingWrapper.Everything_SetReplyID(replyId);
        if (func(_index, out var date) && date >= 0)
        {
            return DateTime.FromFileTime(date);
        }

        return !string.IsNullOrEmpty(FullPath) ? fallbackDelegate(FullPath) : DateTime.MinValue;
    }
}