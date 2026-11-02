// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 业务附件的文件。
/// </summary>
/// <remarks>
/// 存在调用方所在的一侧，访问级别「需要授权」、记上业务给的来源；签名不核对通用的文件权限，由业务按自己的范围先核对（方案 H2）。
/// </remarks>
/// <param name="files">文件服务。</param>
public sealed class AttachmentFiles(FileService files) : IAttachmentFiles
{
    private static readonly FileDetails _details = new(FileAccessLevel.Authorized, IsTemporary: false, ExpiresAt: null, Tags: null, Remark: null);

    /// <inheritdoc />
    public async Task<Result<AttachmentFile>> UploadAsync(InvocationContext context, string source, Stream content, string? fileName, string? contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var uploaded = await files.UploadAsync(context, content, fileName, contentType, _details, source: source).ConfigureAwait(false);
        return uploaded.TryGetValue(out var file) ? From(file) : uploaded.Error!;
    }

    /// <inheritdoc />
    public async Task<Result<Maybe<AttachmentFile>>> FastUploadAsync(InvocationContext context, string source, string hash, long size, string fileName, string? contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var reused = await files.FastUploadAsync(context, hash, size, fileName, contentType, _details, source).ConfigureAwait(false);
        if (!reused.TryGetValue(out var hit))
        {
            return reused.Error!;
        }

        return hit.TryGetValue(out var file) ? Maybe.Some(From(file)) : Maybe.None<AttachmentFile>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, AttachmentFile>> FindAsync(InvocationContext context, IReadOnlyCollection<Guid> fileIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(fileIds);
        var side = SideRows.SideOf(context);
        var found = new Dictionary<Guid, AttachmentFile>();
        foreach (var id in fileIds.Distinct())
        {
            if ((await files.FindAsync(side, id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var file) && file.Status == FileStatus.Normal)
            {
                found[id] = From(file);
            }
        }

        return found;
    }

    /// <inheritdoc />
    public async Task<Result<AttachmentLink>> SignAsync(InvocationContext context, Guid fileId, bool download)
    {
        ArgumentNullException.ThrowIfNull(context);
        var found = await files.FindAsync(SideRows.SideOf(context), fileId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        var signed = await files.SignAsync(file, lifetime: null, download, context.CancellationToken).ConfigureAwait(false);
        return signed.TryGetValue(out var link) ? new AttachmentLink(link.Url, link.ExpiresAt) : signed.Error!;
    }

    private static AttachmentFile From(SysFile file) => new(file.Id, file.OriginalName, file.ContentType, file.Size, file.Source, file.CreatedBy);
}
