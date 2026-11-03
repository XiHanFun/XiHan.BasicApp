// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 业务产出的临时文件。
/// </summary>
/// <remarks>
/// 存在调用方所在的一侧，标为临时、带到期时间（同导出中心的结果文件），访问级别「需要授权」、记上业务给的来源；
/// 读取不核对通用的文件权限，由业务按自己的权限先核对。到期的按不存在处理。
/// </remarks>
/// <param name="files">文件服务。</param>
/// <param name="clock">时钟。</param>
public sealed class TemporaryFiles(FileService files, TimeProvider clock) : ITemporaryFiles
{
    /// <inheritdoc />
    public async Task<Result<AttachmentFile>> SaveAsync(InvocationContext context, string source, Stream content, string fileName, string contentType, TimeSpan lifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        var details = new FileDetails(FileAccessLevel.Authorized, IsTemporary: true, clock.GetUtcNow().Add(lifetime), Tags: source, Remark: null);
        var uploaded = await files.UploadAsync(context, content, fileName, contentType, details, source: source).ConfigureAwait(false);
        return uploaded.TryGetValue(out var file) ? new AttachmentFile(file.Id, file.OriginalName, file.ContentType, file.Size, file.Source, file.CreatedBy) : uploaded.Error!;
    }

    /// <inheritdoc />
    public async Task<Result<TemporaryFileContent>> OpenAsync(InvocationContext context, Guid fileId)
    {
        ArgumentNullException.ThrowIfNull(context);
        var found = await files.FindAsync(SideRows.SideOf(context), fileId, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        if (file.Status != FileStatus.Normal || file.ExpiresAt is { } expires && expires <= clock.GetUtcNow())
        {
            return Error.NotFound(FilesErrors.FileNotFound, FilesMessages.FileNotFound, MessageArguments.Of(("id", fileId)));
        }

        var opened = await files.OpenAsync(file, range: null, context.CancellationToken).ConfigureAwait(false);
        return opened.TryGetValue(out var blob) ? new TemporaryFileContent(file.OriginalName, file.ContentType, blob.Metadata.Length, blob.Content) : opened.Error!;
    }
}
