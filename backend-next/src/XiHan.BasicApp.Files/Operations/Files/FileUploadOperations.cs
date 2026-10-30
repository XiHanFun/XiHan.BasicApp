// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Files.Operations.Files;

/// <summary>
/// 发起一次分片上传。
/// </summary>
/// <remarks>
/// <c>POST /api/files:start-upload</c>。大文件分段直传存储：发起后按 <c>files.sign-upload-parts</c> 签出的地址逐片 PUT，再 <c>files.complete-upload</c>；
/// 小文件仍走普通端点 <c>POST /api/files/upload</c>。大小上限与存储配额同普通上传。
/// </remarks>
/// <param name="uploads">分片上传服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesCreate)]
public sealed class StartUpload(FileUploadService uploads) : IOperation<StartUploadRequest, FileUploadResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.start-upload";

    /// <inheritdoc />
    public async ValueTask<Result<FileUploadResponse>> HandleAsync(StartUploadRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (ExportTaskService.RequesterOf(context) is not { } userId)
        {
            return Error.Denied(SecurityErrors.NotAuthenticated, SecurityMessages.AuthenticationRequired);
        }

        var started = await uploads.StartAsync(context, userId, request.ToDefinition()).ConfigureAwait(false);
        return started.TryGetValue(out var upload) ? FileUploadResponse.From(upload, upload.Status, []) : started.Error!;
    }
}

/// <summary>
/// 取自己的一次分片上传。
/// </summary>
/// <remarks>
/// <c>GET /api/files:upload-session?uploadId=</c>。上传中的带上存储里已上传的分片编号，断点续传时据此跳过已传的分片。
/// </remarks>
/// <param name="uploads">分片上传服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = FilesPermissions.FilesCreate)]
public sealed class GetUploadSession(FileUploadService uploads) : IOperation<UploadSessionRequest, FileUploadResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.upload-session";

    /// <inheritdoc />
    public async ValueTask<Result<FileUploadResponse>> HandleAsync(UploadSessionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await FileUploadLookup.FindAsync(uploads, context, request.UploadId).ConfigureAwait(false);
        if (!found.TryGetValue(out var upload))
        {
            return found.Error!;
        }

        var parts = await uploads.UploadedPartsAsync(upload, context.CancellationToken).ConfigureAwait(false);
        if (!parts.TryGetValue(out var uploaded))
        {
            return parts.Error!;
        }

        return FileUploadResponse.From(upload, uploads.StatusOf(upload), [.. uploaded.Select(static p => p.PartNumber)]);
    }
}

/// <summary>
/// 签出分片的上传地址。
/// </summary>
/// <remarks>
/// <c>POST /api/files:sign-upload-parts</c>。一次至多 100 片；地址缺省 30 分钟到期，过期了再签。
/// </remarks>
/// <param name="uploads">分片上传服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesCreate)]
public sealed class SignUploadParts(FileUploadService uploads) : IOperation<SignUploadPartsRequest, SignUploadPartsResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.sign-upload-parts";

    /// <inheritdoc />
    public async ValueTask<Result<SignUploadPartsResponse>> HandleAsync(SignUploadPartsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await FileUploadLookup.FindAsync(uploads, context, request.UploadId).ConfigureAwait(false);
        if (!found.TryGetValue(out var upload))
        {
            return found.Error!;
        }

        var signed = await uploads.SignPartsAsync(upload, request.PartNumbers, context.CancellationToken).ConfigureAwait(false);
        return signed.TryGetValue(out var parts)
            ? new SignUploadPartsResponse([.. parts.Select(static p => new SignedPartResponse(p.PartNumber, p.Url, p.ExpiresAt))])
            : signed.Error!;
    }
}

/// <summary>
/// 完成分片上传，得到文件。
/// </summary>
/// <remarks>
/// <c>POST /api/files:complete-upload</c>。按存储里已上传的分片拼成对象（不用客户端报分片清单），缺片或总大小不符可以补传后再完成。
/// </remarks>
/// <param name="uploads">分片上传服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesCreate)]
public sealed class CompleteUpload(FileUploadService uploads) : IOperation<UploadSessionRequest, FileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.complete-upload";

    /// <inheritdoc />
    public async ValueTask<Result<FileResponse>> HandleAsync(UploadSessionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await FileUploadLookup.FindAsync(uploads, context, request.UploadId).ConfigureAwait(false);
        if (!found.TryGetValue(out var upload))
        {
            return found.Error!;
        }

        var completed = await uploads.CompleteAsync(context, upload).ConfigureAwait(false);
        return completed.TryGetValue(out var file) ? FileResponse.From(file) : completed.Error!;
    }
}

/// <summary>
/// 放弃分片上传。
/// </summary>
/// <remarks>
/// <c>POST /api/files:abort-upload</c>。丢弃存储里已上传的分片；已结束的不能放弃。
/// </remarks>
/// <param name="uploads">分片上传服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesCreate)]
public sealed class AbortUpload(FileUploadService uploads) : IOperation<UploadSessionRequest, FileUploadResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.abort-upload";

    /// <inheritdoc />
    public async ValueTask<Result<FileUploadResponse>> HandleAsync(UploadSessionRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await FileUploadLookup.FindAsync(uploads, context, request.UploadId).ConfigureAwait(false);
        if (!found.TryGetValue(out var upload))
        {
            return found.Error!;
        }

        var aborted = await uploads.AbortAsync(upload, context.CancellationToken).ConfigureAwait(false);
        return aborted.TryGetValue(out var ended) ? FileUploadResponse.From(ended, ended.Status, []) : aborted.Error!;
    }
}

/// <summary>
/// 按调用方找自己发起的分片上传。
/// </summary>
internal static class FileUploadLookup
{
    /// <summary>
    /// 取调用方在当前一侧发起的分片上传。
    /// </summary>
    /// <remarks>
    /// 调用方不是登录用户（没有用户 ID）时与找不到同一种回答。
    /// </remarks>
    public static Task<Result<SysFileUpload>> FindAsync(FileUploadService uploads, InvocationContext context, Guid id)
        => ExportTaskService.RequesterOf(context) is { } userId
            ? uploads.FindAsync(context, userId, id)
            : Task.FromResult<Result<SysFileUpload>>(Error.NotFound(FilesErrors.UploadNotFound, FilesMessages.UploadNotFound, MessageArguments.Of(("id", id))));
}
