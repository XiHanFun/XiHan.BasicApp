// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.ObjectStorage.Abstractions;
using XiHan.Framework.ObjectStorage.Abstractions.Blobs;
using XiHan.Framework.ObjectStorage.Abstractions.Signing;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Permissions;

namespace XiHan.BasicApp.Files.Domain.Services;

/// <summary>
/// 文件的附加信息（上传与修改时给）。
/// </summary>
/// <param name="AccessLevel">访问级别。</param>
/// <param name="IsTemporary">是否临时文件。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="Tags">标签（逗号分隔）。</param>
/// <param name="Remark">备注。</param>
public sealed record FileDetails(FileAccessLevel AccessLevel, bool IsTemporary, DateTimeOffset? ExpiresAt, string? Tags, string? Remark);

/// <summary>
/// 文件服务：上传、读取、访问与副本管理。
/// </summary>
/// <remarks>
/// 上传（边写临时文件边算 SHA-256，同一侧已有相同内容就共用对象）、秒传、读取、访问级别、签名地址、归档、删除与副本管理。
/// 与 4.x 不同：相同内容的上传与秒传各得一条新记录、不改写已有的记录；删到最后一条引用才删对象；副本校验按 SHA-256 比对内容（4.x 只看存在与大小）。
/// 下载计数：按身份读整个内容或从头读的范围记一次，签发签名地址记一次（之后经地址的读取不经应用）。
/// </remarks>
/// <param name="rows">文件与副本的读写。</param>
/// <param name="configs">存储配置。</param>
/// <param name="stores">存储配置对应的对象存储实例。</param>
/// <param name="permissions">权限判定（访问级别）。</param>
/// <param name="options">文件包配置。</param>
/// <param name="ids">ID 生成器。</param>
/// <param name="clock">时钟。</param>
/// <param name="scopes">作用域工厂（下载计数在独立的短事务里写）。</param>
/// <param name="quotas">租户配额（存储空间；没装 Tenancy 即不限）。</param>
/// <param name="logger">日志。</param>
public sealed partial class FileService(
    SideRows rows,
    StorageConfigService configs,
    StorageInstances stores,
    IPermissionChecker permissions,
    FilesOptions options,
    IIdGenerator<Guid> ids,
    TimeProvider clock,
    IServiceScopeFactory scopes,
    IEnumerable<ITenantQuota> quotas,
    ILogger<FileService> logger)
{
    /// <summary>
    /// 来源：上传。
    /// </summary>
    public const string SourceUpload = "upload";

    /// <summary>
    /// 来源：秒传。
    /// </summary>
    public const string SourceFastUpload = "fast-upload";

    private const int BufferSize = 81920;

    /// <summary>
    /// 一侧的文件。
    /// </summary>
    /// <param name="side">侧。</param>
    /// <param name="id">文件 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>文件；没有为失败。</returns>
    public async Task<Result<SysFile>> FindAsync(string side, Guid id, CancellationToken cancellationToken)
        => (await rows.FindAsync<SysFile>(side, id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var file)
            ? file
            : Error.NotFound(FilesErrors.FileNotFound, FilesMessages.FileNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 上传一个文件。
    /// </summary>
    /// <remarks>
    /// 内容先写到临时文件并算 SHA-256，超过上限即停；同一侧已有相同内容的正常文件就共用它的对象，否则写进上传目标配置。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="content">内容。</param>
    /// <param name="fileName">原始文件名。</param>
    /// <param name="contentType">上传方给的内容类型。</param>
    /// <param name="details">附加信息。</param>
    /// <param name="side">存到哪一侧；为空即调用方所在的一侧（头像固定存平台侧，不计入租户的存储配额）。</param>
    /// <returns>新文件。</returns>
    public async Task<Result<SysFile>> UploadAsync(InvocationContext context, Stream content, string? fileName, string? contentType, FileDetails details, string? side = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(details);
        var name = NameOf(fileName);
        if (name is null)
        {
            return Error.Validation(FilesErrors.FileEmpty, FilesMessages.FileEmpty);
        }

        var cancellationToken = context.CancellationToken;
        var temp = Path.Combine(Path.GetTempPath(), "xihan-upload-" + Guid.NewGuid().ToString("N"));
        try
        {
            var spooled = await SpoolAsync(content, temp, cancellationToken).ConfigureAwait(false);
            if (!spooled.TryGetValue(out var written))
            {
                return spooled.Error!;
            }

            var target = side ?? SideRows.SideOf(context);
            var allowed = await EnsureQuotaAsync(context, target, written.Size).ConfigureAwait(false);
            if (allowed.IsFailure)
            {
                return allowed.Error!;
            }

            var reused = await ReuseAsync(target, written.Hash, written.Size, name, contentType, details, SourceUpload, cancellationToken).ConfigureAwait(false);
            if (reused.TryGetValue(out var copy))
            {
                return copy;
            }

            return await StoreAsync(target, temp, written.Size, written.Hash, name, contentType, details, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    /// <summary>
    /// 秒传：同一侧已有相同内容的正常文件时直接得到一条新记录。
    /// </summary>
    /// <remarks>
    /// 内容相同即 SHA-256 与大小都相同，新记录共用它的对象；没有为无值，前端再走普通上传。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="hash">SHA-256。</param>
    /// <param name="size">大小。</param>
    /// <param name="fileName">原始文件名。</param>
    /// <param name="contentType">内容类型。</param>
    /// <param name="details">附加信息。</param>
    /// <returns>新文件；没命中为无值。</returns>
    public async Task<Result<Maybe<SysFile>>> FastUploadAsync(InvocationContext context, string hash, long size, string fileName, string? contentType, FileDetails details)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(details);
        var normalized = hash.Trim().ToLowerInvariant();
        if (normalized.Length != 64 || !normalized.All(char.IsAsciiHexDigitLower))
        {
            return Error.Validation(FilesErrors.HashInvalid, FilesMessages.HashInvalid);
        }

        var name = NameOf(fileName);
        if (name is null || size <= 0)
        {
            return Error.Validation(FilesErrors.FileEmpty, FilesMessages.FileEmpty);
        }

        if (size > options.MaxUploadBytes)
        {
            return Error.Validation(FilesErrors.FileTooLarge, FilesMessages.FileTooLarge, MessageArguments.Of(("max", options.MaxUploadBytes)));
        }

        // 秒传也计入存储配额（4.x 不计）：用量按记录算，共用对象的文件各自占空间
        var side = SideRows.SideOf(context);
        var allowed = await EnsureQuotaAsync(context, side, size).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        return Result.Success(await ReuseAsync(side, normalized, size, name, contentType, details, SourceFastUpload, context.CancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// 判定调用方能否换取这个文件的下载地址。
    /// </summary>
    /// <remarks>
    /// 访问级别「需要授权」的要持有 <c>files.files.read</c>，「已登录」的同一侧已登录即可。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="file">文件。</param>
    /// <returns>能为成功。</returns>
    public async Task<Result> CheckAccessAsync(InvocationContext context, SysFile file)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(file);
        if (file.AccessLevel == FileAccessLevel.Authorized && !await permissions.HasPermissionAsync(context.GetSecurity(), FilesPermissions.FilesRead, context.CancellationToken).ConfigureAwait(false))
        {
            return Error.Denied(FilesErrors.FileAccessDenied, FilesMessages.FileAccessDenied);
        }

        return Result.Success();
    }

    /// <summary>
    /// 签发主副本的下载地址，并记一次下载。
    /// </summary>
    /// <remarks>
    /// 地址由对象存储签出（ADR-0049）：S3 兼容存储是直连存储的完整地址，本地存储是宿主签名端点的路径（<c>/blobs/…</c>，调用方按接口地址拼完整地址）。
    /// 地址指向对象本身、有效期内不再经过应用：签出之后归档、损坏或改访问级别都不影响它，对象删掉（删到最后一条引用）即失效。
    /// </remarks>
    /// <param name="file">文件。</param>
    /// <param name="lifetime">有效期；为空取缺省值。</param>
    /// <param name="download">是否以附件下载（带原始文件名）；否则按内容类型内联显示。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>地址与到期时间。</returns>
    public async Task<Result<(string Url, DateTimeOffset ExpiresAt)>> SignAsync(SysFile file, TimeSpan? lifetime, bool download, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var span = lifetime ?? options.LinkLifetime;
        if (span < TimeSpan.FromMinutes(1) || span > options.MaxLinkLifetime)
        {
            return Error.Validation(FilesErrors.LinkLifetimeInvalid, FilesMessages.LinkLifetimeInvalid, MessageArguments.Of(("min", 60), ("max", (long)options.MaxLinkLifetime.TotalSeconds)));
        }

        var located = await LocateAsync(file, cancellationToken).ConfigureAwait(false);
        if (!located.TryGetValue(out var target))
        {
            return located.Error!;
        }

        var request = BlobUrlRequest.ForDownload(span, download ? file.OriginalName : null);
        var signed = await stores.SignerOf(target.Config, target.Store).SignAsync(target.Storage.Bucket, target.Storage.Key, request, cancellationToken).ConfigureAwait(false);
        if (!signed.TryGetValue(out var url))
        {
            return signed.Error!;
        }

        await CountDownloadAsync(file, cancellationToken).ConfigureAwait(false);
        return (url.Url.OriginalString, url.ExpiresAt);
    }

    /// <summary>
    /// 读主副本的内容。
    /// </summary>
    /// <remarks>
    /// 给了范围就只读那一段（音视频拖动、断点续传），起点不在内容里是 <see cref="ObjectStorageErrors.RangeNotSatisfiable"/>。
    /// 读整个内容或从头读的范围记一次下载：计数在独立的短事务里原子加一，不随下载期间的调用事务一直占着行。
    /// </remarks>
    /// <param name="file">文件。</param>
    /// <param name="range">要读的范围；为空读整个内容。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>内容（调用方释放）。</returns>
    public async Task<Result<BlobContent>> OpenAsync(SysFile file, BlobRange? range, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var located = await LocateAsync(file, cancellationToken).ConfigureAwait(false);
        if (!located.TryGetValue(out var target))
        {
            return located.Error!;
        }

        var opened = range is null
            ? await target.Store.GetAsync(target.Storage.Bucket, target.Storage.Key, cancellationToken).ConfigureAwait(false)
            : await target.Store.GetAsync(target.Storage.Bucket, target.Storage.Key, range, cancellationToken).ConfigureAwait(false);
        if (opened.IsFailure)
        {
            return opened.Error!;
        }

        if (range is null || range.Offset == 0)
        {
            await CountDownloadAsync(file, cancellationToken).ConfigureAwait(false);
        }

        return opened;
    }

    /// <summary>
    /// 修改附加信息。
    /// </summary>
    /// <param name="file">文件。</param>
    /// <param name="details">附加信息。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task UpdateAsync(SysFile file, FileDetails details, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(details);
        Apply(file, details);
        await rows.UpdateAsync(file, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 归档或恢复文件。
    /// </summary>
    /// <remarks>
    /// 只在正常与已归档之间切换；损坏的文件先换主副本。
    /// </remarks>
    /// <param name="side">侧。</param>
    /// <param name="fileIds">文件 ID。</param>
    /// <param name="status">目标状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>逐项结果。</returns>
    public async Task<Result<BatchResult>> SetStatusAsync(string side, IReadOnlyList<Guid> fileIds, FileStatus status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fileIds);
        if (status is not (FileStatus.Normal or FileStatus.Archived))
        {
            return Error.Validation(FilesErrors.FileStatusInvalid, FilesMessages.FileStatusInvalid, MessageArguments.Of(("status", status)));
        }

        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in fileIds.Distinct())
        {
            if (!(await rows.FindAsync<SysFile>(side, id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var file))
            {
                failed.Add(new BatchFailure(id, FilesErrors.FileNotFound));
                continue;
            }

            if (file.Status == FileStatus.Corrupted)
            {
                failed.Add(new BatchFailure(id, FilesErrors.FileUnavailable));
                continue;
            }

            if (file.Status != status)
            {
                file.Status = status;
                await rows.UpdateAsync(file, cancellationToken).ConfigureAwait(false);
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 删除文件：文件与副本软删除。
    /// </summary>
    /// <remarks>
    /// 不再被这一侧其他副本引用的对象随之删除（对象删不掉只记警告，记录照删）。
    /// </remarks>
    /// <param name="side">侧。</param>
    /// <param name="fileIds">文件 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>逐项结果。</returns>
    public async Task<BatchResult> DeleteAsync(string side, IReadOnlyList<Guid> fileIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fileIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in fileIds.Distinct())
        {
            if (!(await rows.FindAsync<SysFile>(side, id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var file))
            {
                failed.Add(new BatchFailure(id, FilesErrors.FileNotFound));
                continue;
            }

            var copies = await rows.Query<SysFileStorage>(side).Where(s => s.FileId == id).ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var copy in copies)
            {
                await rows.SoftDeleteAsync(copy, cancellationToken).ConfigureAwait(false);
            }

            await rows.SoftDeleteAsync(file, cancellationToken).ConfigureAwait(false);
            foreach (var copy in copies)
            {
                await RemoveObjectIfUnusedAsync(side, copy, cancellationToken).ConfigureAwait(false);
            }

            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }

    /// <summary>
    /// 文件的副本（主副本在前）。
    /// </summary>
    /// <param name="file">文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>副本。</returns>
    public async Task<List<SysFileStorage>> StoragesOfAsync(SysFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        var fileId = file.Id;
        var copies = await rows.Query<SysFileStorage>(file.TenantId).Where(s => s.FileId == fileId).ToListAsync(cancellationToken).ConfigureAwait(false);
        return [.. copies.OrderByDescending(static s => s.IsPrimary).ThenBy(static s => s.CreatedAt)];
    }

    /// <summary>
    /// 文件的一个副本。
    /// </summary>
    /// <param name="file">文件。</param>
    /// <param name="storageId">副本 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>副本；没有为失败。</returns>
    public async Task<Result<SysFileStorage>> StorageOfAsync(SysFile file, Guid storageId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        return (await rows.FindAsync<SysFileStorage>(file.TenantId, storageId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var storage) && storage.FileId == file.Id
            ? storage
            : Error.NotFound(FilesErrors.FileStorageNotFound, FilesMessages.FileStorageNotFound, MessageArguments.Of(("id", storageId)));
    }

    /// <summary>
    /// 把一个副本设为主副本。
    /// </summary>
    /// <remarks>
    /// 目标须正常且校验过；损坏的文件随之恢复正常（4.x 同）。
    /// </remarks>
    /// <param name="file">文件。</param>
    /// <param name="storage">副本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> SetPrimaryAsync(SysFile file, SysFileStorage storage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(storage);
        if (storage.Status != FileStorageStatus.Normal || storage.VerifiedAt is null)
        {
            return Error.Conflict(FilesErrors.FileStorageNotUsable, FilesMessages.FileStorageNotUsable);
        }

        foreach (var other in await StoragesOfAsync(file, cancellationToken).ConfigureAwait(false))
        {
            var isTarget = other.Id == storage.Id;
            if (other.IsPrimary != isTarget)
            {
                other.IsPrimary = isTarget;
                await rows.UpdateAsync(other, cancellationToken).ConfigureAwait(false);
            }
        }

        storage.IsPrimary = true;
        if (file.Status == FileStatus.Corrupted)
        {
            file.Status = FileStatus.Normal;
            await rows.UpdateAsync(file, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <summary>
    /// 校验副本：读出内容按 SHA-256 比对。
    /// </summary>
    /// <remarks>
    /// 读不到或不符记校验失败，是主副本时文件记为损坏；通过时记校验时间，主副本通过时损坏的文件恢复正常。
    /// 存储本身不可用（网络、权限）时不改状态，返回存储的错误。
    /// </remarks>
    /// <param name="file">文件。</param>
    /// <param name="storage">副本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> VerifyAsync(SysFile file, SysFileStorage storage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(storage);
        var store = await StoreOfAsync(storage, cancellationToken).ConfigureAwait(false);
        if (!store.TryGetValue(out var blobs))
        {
            return store.Error!;
        }

        string? problem;
        var opened = await blobs.GetAsync(storage.Bucket, storage.Key, cancellationToken).ConfigureAwait(false);
        if (opened.TryGetValue(out var blob))
        {
            await using (blob.ConfigureAwait(false))
            {
                var (hash, _) = await DigestAsync(blob.Content, cancellationToken).ConfigureAwait(false);
                problem = string.Equals(hash, file.Hash, StringComparison.Ordinal) ? null : "hash-mismatch";
            }
        }
        else if (string.Equals(opened.Error!.Code, ObjectStorageErrors.BlobNotFound, StringComparison.Ordinal))
        {
            problem = "missing";
        }
        else
        {
            return opened.Error!;
        }

        storage.Status = problem is null ? FileStorageStatus.Normal : FileStorageStatus.VerificationFailed;
        storage.VerifiedAt = clock.GetUtcNow();
        storage.Remark = problem;
        await rows.UpdateAsync(storage, cancellationToken).ConfigureAwait(false);
        if (storage.IsPrimary)
        {
            var status = problem is null ? (file.Status == FileStatus.Corrupted ? FileStatus.Normal : file.Status) : FileStatus.Corrupted;
            if (status != file.Status)
            {
                file.Status = status;
                await rows.UpdateAsync(file, cancellationToken).ConfigureAwait(false);
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 启用或停用副本。
    /// </summary>
    /// <remarks>
    /// 主副本不能停用；校验失败的副本要重新校验才回到正常。
    /// </remarks>
    /// <param name="storage">副本。</param>
    /// <param name="status">目标状态。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public async Task<Result> SetStorageStatusAsync(SysFileStorage storage, FileStorageStatus status, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(storage);
        if (status is not (FileStorageStatus.Normal or FileStorageStatus.Unavailable))
        {
            return Error.Validation(FilesErrors.FileStorageStatusInvalid, FilesMessages.FileStorageStatusInvalid, MessageArguments.Of(("status", status)));
        }

        if (status == FileStorageStatus.Unavailable && storage.IsPrimary)
        {
            return Error.Conflict(FilesErrors.FileStorageNotUsable, FilesMessages.PrimaryStorageLocked);
        }

        if (status == FileStorageStatus.Normal && storage.Status == FileStorageStatus.VerificationFailed)
        {
            return Error.Conflict(FilesErrors.FileStorageNotUsable, FilesMessages.FileStorageNotUsable);
        }

        if (storage.Status != status)
        {
            storage.Status = status;
            await rows.UpdateAsync(storage, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <summary>
    /// 副本所在配置的对象存储。
    /// </summary>
    /// <param name="storage">副本。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>对象存储。</returns>
    public async Task<Result<IBlobStore>> StoreOfAsync(SysFileStorage storage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(storage);
        if (!(await configs.FindForSideAsync(storage.StorageConfigId, storage.TenantId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var config))
        {
            return Error.NotFound(FilesErrors.StorageConfigNotFound, FilesMessages.StorageConfigNotFound, MessageArguments.Of(("id", storage.StorageConfigId)));
        }

        return await stores.GetAsync(config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 正常文件的主副本、它的配置与对象存储。
    /// </summary>
    private async Task<Result<Located>> LocateAsync(SysFile file, CancellationToken cancellationToken)
    {
        if (file.Status != FileStatus.Normal || !(await PrimaryOfAsync(file, cancellationToken).ConfigureAwait(false)).TryGetValue(out var storage))
        {
            return Error.Conflict(FilesErrors.FileUnavailable, FilesMessages.FileUnavailable, MessageArguments.Of(("status", file.Status)));
        }

        if (!(await configs.FindForSideAsync(storage.StorageConfigId, storage.TenantId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var config))
        {
            return Error.NotFound(FilesErrors.StorageConfigNotFound, FilesMessages.StorageConfigNotFound, MessageArguments.Of(("id", storage.StorageConfigId)));
        }

        var store = await stores.GetAsync(config, cancellationToken).ConfigureAwait(false);
        return store.TryGetValue(out var blobs) ? new Located(storage, config, blobs) : store.Error!;
    }

    /// <summary>
    /// 在独立的短事务里给下载次数原子加一。
    /// </summary>
    /// <remarks>
    /// 计数失败只记警告，不影响下载本身。
    /// </remarks>
    private async Task CountDownloadAsync(SysFile file, CancellationToken cancellationToken)
    {
        var fileId = file.Id;
        var now = clock.GetUtcNow();
        var counted = await StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
            await unitOfWork.ConnectionFor(typeof(SysFile)).Updateable<SysFile>().SetColumns(f => new SysFile { DownloadCount = f.DownloadCount + 1, LastDownloadedAt = now }).Where(f => f.Id == fileId).ExecuteCommandAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }, cancellationToken).ConfigureAwait(false);
        if (counted.IsFailure)
        {
            LogDownloadNotCounted(logger, fileId, counted.Error!.Code);
            return;
        }

        file.DownloadCount++;
        file.LastDownloadedAt = now;
    }

    /// <summary>
    /// 把分片上传拼好的对象落成文件。
    /// </summary>
    /// <remarks>
    /// 内容按回读算出的 SHA-256 记；同一侧已有内容相同的正常文件就共用它的对象，刚拼好的对象随即删掉（删不掉只记警告），
    /// 否则以发起时定下的文件 ID 新建文件与指向这个对象的主副本。
    /// </remarks>
    /// <param name="upload">分片上传。</param>
    /// <param name="store">它的对象存储。</param>
    /// <param name="hash">回读算出的 SHA-256。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>新文件。</returns>
    internal async Task<SysFile> AdoptAsync(SysFileUpload upload, IBlobStore store, string hash, CancellationToken cancellationToken)
    {
        var details = new FileDetails(upload.AccessLevel, upload.IsTemporary, upload.FileExpiresAt, upload.Tags, upload.Remark);
        var reused = await ReuseAsync(upload.TenantId, hash, upload.Size, upload.OriginalName, upload.ContentType, details, SourceUpload, cancellationToken).ConfigureAwait(false);
        if (reused.TryGetValue(out var copy))
        {
            var removed = await store.DeleteAsync(upload.Bucket, upload.Key, cancellationToken).ConfigureAwait(false);
            if (removed.Error is { } error)
            {
                LogDuplicateNotRemoved(logger, upload.Id, upload.Bucket, upload.Key, error.Code);
            }

            return copy;
        }

        var file = NewFile(upload.TenantId, upload.OriginalName, upload.ContentType, upload.Size, hash, details, SourceUpload, upload.FileId);
        await rows.InsertAsync(file, cancellationToken).ConfigureAwait(false);
        await rows.InsertAsync(new SysFileStorage
        {
            Id = ids.NewId(),
            TenantId = upload.TenantId,
            FileId = file.Id,
            StorageConfigId = upload.StorageConfigId,
            StorageType = upload.StorageType,
            Bucket = upload.Bucket,
            Key = upload.Key,
            IsPrimary = true,
            Status = FileStorageStatus.Normal,
            VerifiedAt = clock.GetUtcNow(),
        }, cancellationToken).ConfigureAwait(false);
        return file;
    }

    /// <summary>
    /// 核对一侧的存储配额。
    /// </summary>
    /// <remarks>
    /// 只约束租户主体的调用；没装 Tenancy 即不限。
    /// </remarks>
    internal async Task<Result> EnsureQuotaAsync(InvocationContext context, string side, long bytes)
    {
        foreach (var quota in quotas)
        {
            var allowed = await quota.EnsureStorageAsync(context, side, bytes).ConfigureAwait(false);
            if (allowed.IsFailure)
            {
                return allowed;
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// 规范原始文件名。
    /// </summary>
    /// <remarks>
    /// 去掉路径与首尾空白，过长的截掉主名、保留扩展名；为空时为 <see langword="null"/>。
    /// </remarks>
    internal static string? NameOf(string? fileName)
    {
        var name = Path.GetFileName(fileName?.Trim() ?? string.Empty);
        if (name.Length == 0)
        {
            return null;
        }

        if (name.Length <= 255)
        {
            return name;
        }

        // 过长的名字截掉主名，保留扩展名
        var extension = FileKinds.ExtensionOf(name) ?? string.Empty;
        return name[..(255 - extension.Length)] + extension;
    }

    /// <summary>
    /// 读完内容，算 SHA-256 与长度。
    /// </summary>
    internal static async Task<(string Hash, long Length)> DigestAsync(Stream content, CancellationToken cancellationToken)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long length = 0;
        try
        {
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
            {
                hasher.AppendData(buffer, 0, read);
                length += read;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return (Convert.ToHexStringLower(hasher.GetHashAndReset()), length);
    }

    private static void Apply(SysFile file, FileDetails details)
    {
        file.AccessLevel = details.AccessLevel;
        file.IsTemporary = details.IsTemporary;
        file.ExpiresAt = details.ExpiresAt;
        file.Tags = Normalize(details.Tags);
        file.Remark = string.IsNullOrWhiteSpace(details.Remark) ? null : details.Remark.Trim();
    }

    private static string? Normalize(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return null;
        }

        var parts = tags.Split([',', '，'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal);
        var joined = string.Join(',', parts);
        return joined.Length == 0 ? null : joined;
    }

    private async Task<Result<(long Size, string Hash)>> SpoolAsync(Stream content, string path, CancellationToken cancellationToken)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        long size = 0;
        try
        {
            var spool = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
            await using (spool.ConfigureAwait(false))
            {
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    size += read;
                    if (size > options.MaxUploadBytes)
                    {
                        return Error.Validation(FilesErrors.FileTooLarge, FilesMessages.FileTooLarge, MessageArguments.Of(("max", options.MaxUploadBytes)));
                    }

                    hasher.AppendData(buffer, 0, read);
                    await spool.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        if (size == 0)
        {
            return Error.Validation(FilesErrors.FileEmpty, FilesMessages.FileEmpty);
        }

        return (size, Convert.ToHexStringLower(hasher.GetHashAndReset()));
    }

    /// <summary>
    /// 复用同一侧内容相同的正常文件的对象。
    /// </summary>
    /// <remarks>
    /// 同一侧有内容相同的正常文件（主副本正常）就复制出一条新记录与一个指向同一对象的主副本。
    /// </remarks>
    private async Task<Maybe<SysFile>> ReuseAsync(string side, string hash, long size, string name, string? contentType, FileDetails details, string source, CancellationToken cancellationToken)
    {
        var candidates = await rows.Query<SysFile>(side).Where(f => f.Hash == hash && f.Size == size && f.Status == FileStatus.Normal).Take(5).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var candidate in candidates)
        {
            if (!(await PrimaryOfAsync(candidate, cancellationToken).ConfigureAwait(false)).TryGetValue(out var primary) || primary.Status != FileStorageStatus.Normal)
            {
                continue;
            }

            var file = NewFile(side, name, contentType, size, hash, details, source);
            await rows.InsertAsync(file, cancellationToken).ConfigureAwait(false);
            await rows.InsertAsync(new SysFileStorage
            {
                Id = ids.NewId(),
                TenantId = side,
                FileId = file.Id,
                StorageConfigId = primary.StorageConfigId,
                StorageType = primary.StorageType,
                Bucket = primary.Bucket,
                Key = primary.Key,
                IsPrimary = true,
                Status = FileStorageStatus.Normal,
                VerifiedAt = primary.VerifiedAt,
            }, cancellationToken).ConfigureAwait(false);
            return Maybe.Some(file);
        }

        return Maybe.None<SysFile>();
    }

    private async Task<Result<SysFile>> StoreAsync(string side, string temp, long size, string hash, string name, string? contentType, FileDetails details, CancellationToken cancellationToken)
    {
        var target = await configs.UploadTargetAsync(side, cancellationToken).ConfigureAwait(false);
        if (!target.TryGetValue(out var config))
        {
            return target.Error!;
        }

        var store = await stores.GetAsync(config, cancellationToken).ConfigureAwait(false);
        if (!store.TryGetValue(out var blobs))
        {
            return store.Error!;
        }

        var file = NewFile(side, name, contentType, size, hash, details, SourceUpload);
        var now = clock.GetUtcNow();
        var key = $"{side}/{now:yyyy}/{now:MM}/{now:dd}/{file.Id:N}{file.Extension}";
        var put = new BlobPutOptions
        {
            ContentType = file.ContentType,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal) { ["file-id"] = file.Id.ToString("N"), ["sha256"] = hash },
            Overwrite = false,
        };
        var source = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using (source.ConfigureAwait(false))
        {
            var written = await blobs.PutAsync(config.Bucket, key, source, put, cancellationToken).ConfigureAwait(false);
            if (written.IsFailure)
            {
                return written.Error!;
            }
        }

        await rows.InsertAsync(file, cancellationToken).ConfigureAwait(false);
        await rows.InsertAsync(new SysFileStorage
        {
            Id = ids.NewId(),
            TenantId = side,
            FileId = file.Id,
            StorageConfigId = config.Id,
            StorageType = config.Type,
            Bucket = config.Bucket,
            Key = key,
            IsPrimary = true,
            Status = FileStorageStatus.Normal,
            VerifiedAt = now,
        }, cancellationToken).ConfigureAwait(false);
        return file;
    }

    private SysFile NewFile(string side, string name, string? contentType, long size, string hash, FileDetails details, string source, Guid? fileId = null)
    {
        var extension = FileKinds.ExtensionOf(name);
        var type = FileKinds.ContentTypeOf(contentType, name);
        var id = fileId ?? ids.NewId();
        var file = new SysFile
        {
            Id = id,
            TenantId = side,
            Name = id.ToString("N") + extension,
            OriginalName = name,
            Extension = extension,
            ContentType = type,
            Kind = FileKinds.KindOf(type, extension),
            Size = size,
            Hash = hash,
            Status = FileStatus.Normal,
            Source = source,
        };
        Apply(file, details);
        return file;
    }

    private async Task<Maybe<SysFileStorage>> PrimaryOfAsync(SysFile file, CancellationToken cancellationToken)
    {
        var fileId = file.Id;
        var primary = await rows.Query<SysFileStorage>(file.TenantId).Where(s => s.FileId == fileId && s.IsPrimary).Take(1).ToListAsync(cancellationToken).ConfigureAwait(false);
        return primary.Count == 0 ? Maybe.None<SysFileStorage>() : Maybe.Some(primary[0]);
    }

    private async Task RemoveObjectIfUnusedAsync(string side, SysFileStorage copy, CancellationToken cancellationToken)
    {
        var configId = copy.StorageConfigId;
        var bucket = copy.Bucket;
        var key = copy.Key;
        var others = await rows.Query<SysFileStorage>(side).Where(s => s.StorageConfigId == configId && s.Bucket == bucket && s.Key == key).CountAsync(cancellationToken).ConfigureAwait(false);
        if (others > 0)
        {
            return;
        }

        var store = await StoreOfAsync(copy, cancellationToken).ConfigureAwait(false);
        var error = store.Error;
        if (store.TryGetValue(out var blobs))
        {
            var removed = await blobs.DeleteAsync(copy.Bucket, copy.Key, cancellationToken).ConfigureAwait(false);
            error = removed.Error;
        }

        if (error is not null)
        {
            LogObjectNotRemoved(logger, copy.Id, copy.Bucket, copy.Key, error.Code);
        }
    }

    private sealed record Located(SysFileStorage Storage, SysStorageConfig Config, IBlobStore Store);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "The object {Bucket}/{Key} assembled by upload {UploadId} duplicates stored content and was not removed ({Code}).")]
    private static partial void LogDuplicateNotRemoved(ILogger logger, Guid uploadId, string bucket, string key, string code);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "The download of file {FileId} was not counted ({Code}).")]
    private static partial void LogDownloadNotCounted(ILogger logger, Guid fileId, string code);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The object {Bucket}/{Key} of file copy {StorageId} was not removed ({Code}); the records are deleted.")]
    private static partial void LogObjectNotRemoved(ILogger logger, Guid storageId, string bucket, string key, string code);
}
