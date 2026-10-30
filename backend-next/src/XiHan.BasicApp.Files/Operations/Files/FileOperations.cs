// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Files.Queries;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Operations.Files;

/// <summary>
/// 分页列出当前一侧的文件。
/// </summary>
/// <remarks>
/// <c>POST /api/files:list</c>。上传与下载内容走普通端点 <c>POST /api/files/upload</c>、<c>GET /api/files/{id}/content</c>。
/// </remarks>
/// <param name="queries">文件查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesRead)]
public sealed class ListFiles(FileQueries queries) : IOperation<PageRequest, PageResult<FileResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<FileResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 按 ID 取当前一侧的文件：<c>GET /api/files/{id}</c>。
/// </summary>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesRead)]
public sealed class GetFile(FileService files) : IOperation<GetFileRequest, FileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.get";

    /// <inheritdoc />
    public async ValueTask<Result<FileResponse>> HandleAsync(GetFileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await files.FindAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        return FileResponse.From(file);
    }
}

/// <summary>
/// 修改文件的访问级别、临时与到期、标签与备注。
/// </summary>
/// <remarks>
/// <c>PUT /api/files/{id}</c>。
/// </remarks>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesUpdate)]
public sealed class UpdateFile(FileService files) : IOperation<UpdateFileRequest, FileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.update";

    /// <inheritdoc />
    public async ValueTask<Result<FileResponse>> HandleAsync(UpdateFileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await files.FindAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        await files.UpdateAsync(file, request.ToDetails(), context.CancellationToken).ConfigureAwait(false);
        return FileResponse.From(file);
    }
}

/// <summary>
/// 归档或恢复文件。
/// </summary>
/// <remarks>
/// <c>POST /api/files:set-status</c>。已损坏的文件逐项拒绝（先换一个校验通过的主副本）。
/// </remarks>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesUpdate)]
public sealed class SetFileStatus(FileService files) : IOperation<SetFileStatusRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.set-status";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(SetFileStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await files.SetStatusAsync(SideRows.SideOf(context), request.Ids, request.Status, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 删除文件。
/// </summary>
/// <remarks>
/// <c>DELETE /api/files?ids=</c>。不再被别的文件用到的内容随之从存储里删除。
/// </remarks>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesDelete)]
public sealed class DeleteFiles(FileService files) : IOperation<FileIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(FileIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await files.DeleteAsync(SideRows.SideOf(context), request.Ids, context.CancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// 换取文件的签名下载地址。
/// </summary>
/// <remarks>
/// <c>GET /api/files/{id}:presign</c>。访问级别「已登录」的同一侧已登录即可，「需要授权」的要持有 <c>files.files.read</c>，
/// 「公开」的（头像，固定在平台侧）任何已登录主体不分侧都能换（本侧找不到再到平台侧找，只认公开级别）；
/// 归档或损坏的文件换不到地址。地址由对象存储签出、指向对象本身，有效期内不再校验身份（给图片、链接与新窗口用），签出之后的归档不影响它；换一次记一次下载。
/// </remarks>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class PresignFile(FileService files) : IOperation<PresignFileRequest, PresignFileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.presign";

    /// <inheritdoc />
    public async ValueTask<Result<PresignFileResponse>> HandleAsync(PresignFileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var side = SideRows.SideOf(context);
        var found = await files.FindAsync(side, request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            if (string.Equals(side, TenantIds.Platform, StringComparison.Ordinal)
                || !(await files.FindAsync(TenantIds.Platform, request.Id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var shared)
                || shared.AccessLevel != FileAccessLevel.Public)
            {
                return found.Error!;
            }

            file = shared;
        }

        var allowed = await files.CheckAccessAsync(context, file).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var issued = await files.SignAsync(file, request.ExpiresInSeconds is { } seconds ? TimeSpan.FromSeconds(seconds) : null, request.Download == true, context.CancellationToken).ConfigureAwait(false);
        if (!issued.TryGetValue(out var link))
        {
            return issued.Error!;
        }

        return new PresignFileResponse(link.Url, link.ExpiresAt);
    }
}

/// <summary>
/// 秒传：同一侧已有相同内容时直接得到一条新记录。
/// </summary>
/// <remarks>
/// <c>POST /api/files:fast-upload</c>。同一侧已有内容相同的正常文件时直接得到一条新记录（共用存储里的内容），没命中返回空，前端再走普通上传。
/// </remarks>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesCreate)]
public sealed class FastUploadFile(FileService files) : IOperation<FastUploadFileRequest, FastUploadFileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.fast-upload";

    /// <inheritdoc />
    public async ValueTask<Result<FastUploadFileResponse>> HandleAsync(FastUploadFileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var reused = await files.FastUploadAsync(context, request.Hash, request.Size, request.OriginalName, request.ContentType, request.ToDetails()).ConfigureAwait(false);
        if (!reused.TryGetValue(out var hit))
        {
            return reused.Error!;
        }

        return new FastUploadFileResponse(hit.TryGetValue(out var file) ? FileResponse.From(file) : null);
    }
}

/// <summary>
/// 列出文件的副本（主副本在前）。
/// </summary>
/// <remarks>
/// <c>GET /api/files/{id}:storages</c>。
/// </remarks>
/// <param name="files">文件服务。</param>
/// <param name="configs">存储配置（补配置的编码与名称）。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = FilesPermissions.FilesRead)]
public sealed class ListFileStorages(FileService files, StorageConfigService configs) : IOperation<GetFileRequest, FileStoragesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.storages";

    /// <inheritdoc />
    public async ValueTask<Result<FileStoragesResponse>> HandleAsync(GetFileRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await files.FindAsync(SideRows.SideOf(context), request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        var copies = await files.StoragesOfAsync(file, context.CancellationToken).ConfigureAwait(false);
        var known = await configs.ManyAsync([.. copies.Select(static s => s.StorageConfigId).Distinct()], context.CancellationToken).ConfigureAwait(false);
        return new FileStoragesResponse([.. copies.Select(s =>
        {
            var config = known.GetValueOrDefault(s.StorageConfigId);
            return new FileStorageResponse(s.Id, s.StorageConfigId, config?.Code, config?.Name, s.StorageType, s.Bucket, s.Key, s.IsPrimary, s.Status, s.VerifiedAt, s.Remark, s.CreatedAt);
        })]);
    }
}

/// <summary>
/// 把一个副本设为主副本。
/// </summary>
/// <remarks>
/// <c>POST /api/files/{id}:set-primary-storage</c>。目标须正常且校验过；已损坏的文件随之恢复。
/// </remarks>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesUpdate)]
public sealed class SetPrimaryStorage(FileService files) : IOperation<FileStorageRequest, FileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.set-primary-storage";

    /// <inheritdoc />
    public async ValueTask<Result<FileResponse>> HandleAsync(FileStorageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var found = await files.FindAsync(SideRows.SideOf(context), request.Id, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        var storage = await files.StorageOfAsync(file, request.StorageId, cancellationToken).ConfigureAwait(false);
        if (!storage.TryGetValue(out var target))
        {
            return storage.Error!;
        }

        var changed = await files.SetPrimaryAsync(file, target, cancellationToken).ConfigureAwait(false);
        if (changed.IsFailure)
        {
            return changed.Error!;
        }

        return FileResponse.From(file);
    }
}

/// <summary>
/// 校验一个副本。
/// </summary>
/// <remarks>
/// <c>POST /api/files/{id}:verify-storage</c>。读出内容按 SHA-256 比对，不符或读不到记校验失败，主副本不通过时文件记为损坏。
/// </remarks>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesUpdate)]
public sealed class VerifyStorage(FileService files) : IOperation<FileStorageRequest, FileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.verify-storage";

    /// <inheritdoc />
    public async ValueTask<Result<FileResponse>> HandleAsync(FileStorageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var found = await files.FindAsync(SideRows.SideOf(context), request.Id, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        var storage = await files.StorageOfAsync(file, request.StorageId, cancellationToken).ConfigureAwait(false);
        if (!storage.TryGetValue(out var target))
        {
            return storage.Error!;
        }

        var verified = await files.VerifyAsync(file, target, cancellationToken).ConfigureAwait(false);
        if (verified.IsFailure)
        {
            return verified.Error!;
        }

        return FileResponse.From(file);
    }
}

/// <summary>
/// 启用或停用一个副本。
/// </summary>
/// <remarks>
/// <c>POST /api/files/{id}:set-storage-status</c>。主副本不能停用，校验失败的副本要重新校验。
/// </remarks>
/// <param name="files">文件服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = FilesPermissions.FilesUpdate)]
public sealed class SetStorageStatus(FileService files) : IOperation<SetFileStorageStatusRequest, FileResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "files.set-storage-status";

    /// <inheritdoc />
    public async ValueTask<Result<FileResponse>> HandleAsync(SetFileStorageStatusRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var cancellationToken = context.CancellationToken;
        var found = await files.FindAsync(SideRows.SideOf(context), request.Id, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var file))
        {
            return found.Error!;
        }

        var storage = await files.StorageOfAsync(file, request.StorageId, cancellationToken).ConfigureAwait(false);
        if (!storage.TryGetValue(out var target))
        {
            return storage.Error!;
        }

        var changed = await files.SetStorageStatusAsync(target, request.Status, cancellationToken).ConfigureAwait(false);
        if (changed.IsFailure)
        {
            return changed.Error!;
        }

        return FileResponse.From(file);
    }
}
