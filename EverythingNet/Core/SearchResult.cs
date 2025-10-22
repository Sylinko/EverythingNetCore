using System.Diagnostics.CodeAnalysis;

namespace EverythingNet.Core;

using System;
using System.IO;
using System.Text;
using Interfaces;

internal class SearchResult(int index, uint replyId) : ISearchResult
{
    private delegate bool MyDelegate(uint index, out long date);

    private readonly uint index = Convert.ToUInt32(index);

    public long Index => index;

    public bool IsFile => EverythingWrapper.Everything_IsFileResult(index);

    [field: AllowNull, MaybeNull]
    public string FullPath
    {
        get
        {
            if (field == null)
            {
                var builder = new StringBuilder(260);

                EverythingWrapper.Everything_SetReplyID(replyId);
                EverythingWrapper.Everything_GetResultFullPathName(index, builder, 260);

                field = builder.ToString();
            }

            return field;
        }
    }

    public string Path
    {
        get
        {
            //EverythingWrapper.Everything_SetReplyID(replyId);
            //return EverythingWrapper.Everything_GetResultPath(index);

            // Temporary implementation until the native function works as expected
            try
            {
                return !string.IsNullOrEmpty(FullPath) ? System.IO.Path.GetDirectoryName(FullPath)! : string.Empty;
            }
            catch (Exception e)
            {
                LastException = e;
                return FullPath;
            }
        }
    }

    public string FileName
    {
        get
        {
            //EverythingWrapper.Everything_SetReplyID(replyId);
            //return EverythingWrapper.Everything_GetResultFileName(index);

            // Temporary implementation until the native function works as expected
            try
            {
                return !string.IsNullOrEmpty(FullPath) ? System.IO.Path.GetFileName(FullPath) : string.Empty;
            }
            catch (Exception e)
            {
                LastException = e;
                return FullPath;
            }
        }
    }

    public long Size
    {
        get
        {
            EverythingWrapper.Everything_SetReplyID(replyId);
            EverythingWrapper.Everything_GetResultSize(index, out var size);

            return size;
        }
    }

    public uint Attributes
    {
        get
        {
            EverythingWrapper.Everything_SetReplyID(replyId);
            var attributes = EverythingWrapper.Everything_GetResultAttributes(index);

            return attributes > 0 ? attributes
                : !string.IsNullOrEmpty(FullPath) ? (uint)File.GetAttributes(FullPath)
                : 0;
        }
    }

    public DateTime Created => GenericDate(EverythingWrapper.Everything_GetResultDateCreated, File.GetCreationTime);

    public DateTime Modified => GenericDate(EverythingWrapper.Everything_GetResultDateModified, File.GetLastWriteTime);

    public DateTime Accessed => GenericDate(EverythingWrapper.Everything_GetResultDateAccessed, File.GetLastAccessTime);

    public DateTime Executed => GenericDate(EverythingWrapper.Everything_GetResultDateRun, File.GetLastAccessTime);

    public Exception? LastException { get; private set; }

    private DateTime GenericDate(MyDelegate func, Func<string, DateTime> fallbackDelegate)
    {
        EverythingWrapper.Everything_SetReplyID(replyId);
        if (func(index, out var date) && date >= 0)
        {
            return DateTime.FromFileTime(date);
        }

        return !string.IsNullOrEmpty(FullPath) ? fallbackDelegate(FullPath) : DateTime.MinValue;
    }
}