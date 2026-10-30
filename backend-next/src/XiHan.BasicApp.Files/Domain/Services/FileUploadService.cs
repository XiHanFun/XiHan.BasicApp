// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Infrastructure;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.ObjectStorage.Abstractions.Blobs;
using XiHan.Framework.ObjectStorage.Abstractions.Multipart;
using XiHan.Framework.ObjectStorage.Abstractions.Signing;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Domain.Services;

/// <summary>
/// 发起分片上传时给的内容。
/// </summary>
/// <param name="OriginalName">原始文件名。</param>
/// <param name="Size">大小（字节）。</param>
/// <param name="ContentType">上传方给的内容类型。</param>
/// <param name="Details">完成后文件的附加信息。</param>
public sealed record FileUploadDefinition(string OriginalName, long Size, string? ContentType, FileDetails Details);

/// <summary>
/// 签出的分片上传地址。
/// </summary>
/// <param name="PartNumber">分片编号。</param>
/// <param name="Url">地址。</param>
/// <param name="ExpiresAt">到期时间。</param>
public sealed record SignedPart(int PartNumber, string Url, DateTimeOffset ExpiresAt);

/// <summary>
/// 分片上传的发起、续传、完成与放弃。
/// </summary>
/// <remarks>
/// 各分片经对象存储签出的地址直传（ADR-0049：S3 兼容存储直连存储，本地存储走宿主的 <c>/blobs</c> 端点），不经应用转发。
/// 完成时不用客户端报的分片清单，而按存储列出的分片核对编号齐全、总大小与声明的一致，拼好后回读算 SHA-256（客户端不必自己算），
/// 再经 <see cref="FileService"/> 落文件：存储配额在发起与完成时各核对一次，内容与同一侧已有的相同即共用已有的对象。
/// </remarks>
/// <param name="rows">按侧读写。</param>
/// <param name="configs">存储配置。</param>
/// <param name="stores">存储配置对应的对象存储实例。</param>
/// <param name="files">文件服务。</param>
/// <param name="options">文件包配置。</param>
/// <param name="ids">ID 生成器。</param>
/// <param name="clock">时钟。</param>
/// <param name="logger">日志。</param>
public sealed partial class FileUploadService(
    SideRows rows,
    StorageConfigService configs,
    StorageInstances stores,
    FileService files,
    FilesOptions options,
    IIdGenerator<Guid> ids,
    TimeProvider clock,
    ILogger<FileUploadService> logger)
{
    /// <summary>
    /// 一次至多签出的分片地址数。
    /// </summary>
    public const int MaxSignedParts = 100;

    /// <summary>
    /// 过期处理一批的条数。
    /// </summary>
    public const int ExpireBatchSize = 100;

    /// <summary>
    /// 发起一次分片上传。
    /// </summary>
    /// <remarks>
    /// 大小不超过单个文件的上限、核对存储配额，选当前一侧的上传目标，在它的存储里开一次分片上传；
    /// 每片大小取配置（文件大到超过一万片时按文件大小放大），最后一片可以更小。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="userId">发起的用户。</param>
    /// <param name="definition">发起的内容。</param>
    /// <returns>新的分片上传。</returns>
    public async Task<Result<SysFileUpload>> StartAsync(InvocationContext context, Guid userId, FileUploadDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(definition);
        var name = FileService.NameOf(definition.OriginalName);
        if (name is null || definition.Size <= 0)
        {
            return Error.Validation(FilesErrors.FileEmpty, FilesMessages.FileEmpty);
        }

        if (definition.Size > options.MaxUploadBytes)
        {
            return Error.Validation(FilesErrors.FileTooLarge, FilesMessages.FileTooLarge, MessageArguments.Of(("max", options.MaxUploadBytes)));
        }

        var cancellationToken = context.CancellationToken;
        var side = SideRows.SideOf(context);
        var allowed = await files.EnsureQuotaAsync(context, side, definition.Size).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

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

        if (blobs is not IBlobMultipartStore multipart)
        {
            return Error.Unavailable(FilesErrors.MultipartUnsupported, FilesMessages.MultipartUnsupported, MessageArguments.Of(("code", config.Code)));
        }

        var fileId = ids.NewId();
        var now = clock.GetUtcNow();
        var key = $"{side}/{now:yyyy}/{now:MM}/{now:dd}/{fileId:N}{FileKinds.ExtensionOf(name)}";
        var put = new BlobPutOptions
        {
            ContentType = FileKinds.ContentTypeOf(definition.ContentType, name),
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal) { ["file-id"] = fileId.ToString("N") },
            Overwrite = false,
        };
        var started = await multipart.StartAsync(config.Bucket, key, put, cancellationToken).ConfigureAwait(false);
        if (!started.TryGetValue(out var opened))
        {
            return started.Error!;
        }

        var partSize = Math.Max(options.UploadPartBytes, (definition.Size + BlobMultipart.MaxPartNumber - 1) / BlobMultipart.MaxPartNumber);
        var details = definition.Details;
        var upload = new SysFileUpload
        {
            Id = ids.NewId(),
            TenantId = side,
            UserId = userId,
            FileId = fileId,
            StorageConfigId = config.Id,
            StorageType = config.Type,
            Bucket = config.Bucket,
            Key = key,
            UploadId = opened.UploadId,
            OriginalName = name,
            ContentType = string.IsNullOrWhiteSpace(definition.ContentType) ? null : definition.ContentType.Trim(),
            Size = definition.Size,
            PartSize = partSize,
            PartCount = (int)((definition.Size + partSize - 1) / partSize),
            AccessLevel = details.AccessLevel,
            IsTemporary = details.IsTemporary,
            FileExpiresAt = details.ExpiresAt,
            Tags = details.Tags,
            Remark = details.Remark,
            Status = FileUploadStatus.Uploading,
            ExpiresAt = now.Add(options.UploadLifetime),
        };
        await rows.InsertAsync(upload, cancellationToken).ConfigureAwait(false);
        return upload;
    }

    /// <summary>
    /// 取这个用户在当前一侧发起的一次分片上传。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="userId">发起的用户。</param>
    /// <param name="id">分片上传 ID。</param>
    /// <returns>分片上传；不存在或不是这个用户发起的为失败。</returns>
    public async Task<Result<SysFileUpload>> FindAsync(InvocationContext context, Guid userId, Guid id)
    {
        ArgumentNullException.ThrowIfNull(context);
        return (await rows.FindAsync<SysFileUpload>(SideRows.SideOf(context), id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var upload) && upload.UserId == userId
            ? upload
            : Error.NotFound(FilesErrors.UploadNotFound, FilesMessages.UploadNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 当前的状态（上传中但已过时限的算过期）。
    /// </summary>
    /// <param name="upload">分片上传。</param>
    /// <returns>状态。</returns>
    public FileUploadStatus StatusOf(SysFileUpload upload)
    {
        ArgumentNullException.ThrowIfNull(upload);
        return upload.Status == FileUploadStatus.Uploading && clock.GetUtcNow() > upload.ExpiresAt ? FileUploadStatus.Expired : upload.Status;
    }

    /// <summary>
    /// 存储里已经上传的分片。
    /// </summary>
    /// <remarks>
    /// 只有上传中的才去存储里列，已结束的为空。
    /// </remarks>
    /// <param name="upload">分片上传。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>已上传的分片，按编号升序。</returns>
    public async Task<Result<IReadOnlyList<BlobPart>>> UploadedPartsAsync(SysFileUpload upload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (StatusOf(upload) != FileUploadStatus.Uploading)
        {
            return Result.Success<IReadOnlyList<BlobPart>>([]);
        }

        var located = await LocateAsync(upload, cancellationToken).ConfigureAwait(false);
        if (!located.TryGetValue(out var target))
        {
            return located.Error!;
        }

        return await target.Multipart.ListPartsAsync(MultipartOf(upload), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 签出分片的上传地址。
    /// </summary>
    /// <remarks>
    /// 客户端不带凭据把这一片 PUT 到地址；同一片再传即覆盖。地址有效期取签名下载地址的缺省有效期。
    /// </remarks>
    /// <param name="upload">分片上传。</param>
    /// <param name="partNumbers">分片编号。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>签出的地址。</returns>
    public async Task<Result<IReadOnlyList<SignedPart>>> SignPartsAsync(SysFileUpload upload, IReadOnlyList<int> partNumbers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(upload);
        ArgumentNullException.ThrowIfNull(partNumbers);
        var active = EnsureActive(upload);
        if (active.IsFailure)
        {
            return active.Error!;
        }

        var numbers = partNumbers.Distinct().Order().ToList();
        if (numbers.Count is 0 or > MaxSignedParts || numbers[0] < 1 || numbers[^1] > upload.PartCount)
        {
            return Error.Validation(FilesErrors.PartNumbersInvalid, FilesMessages.PartNumbersInvalid, MessageArguments.Of(("max", upload.PartCount)));
        }

        var located = await LocateAsync(upload, cancellationToken).ConfigureAwait(false);
        if (!located.TryGetValue(out var target))
        {
            return located.Error!;
        }

        var signer = stores.SignerOf(target.Config, target.Store);
        var multipart = MultipartOf(upload);
        var signed = new List<SignedPart>(numbers.Count);
        foreach (var number in numbers)
        {
            var url = await signer.SignAsync(upload.Bucket, upload.Key, BlobUrlRequest.ForPart(multipart, number, options.LinkLifetime), cancellationToken).ConfigureAwait(false);
            if (!url.TryGetValue(out var link))
            {
                return url.Error!;
            }

            signed.Add(new SignedPart(number, link.Url.OriginalString, link.ExpiresAt));
        }

        return signed;
    }

    /// <summary>
    /// 完成分片上传，得到文件。
    /// </summary>
    /// <remarks>
    /// 按存储列出的分片核对编号齐全（缺的片 <c>FILES_CONFLICT_009</c>）、总大小与声明的一致（<c>FILES_CONFLICT_010</c>），这两种可以补传后再完成；
    /// 再核对一次配额，拼成对象、回读算 SHA-256 与大小，经 <see cref="FileService"/> 落文件，上传记为已完成并指向得到的文件。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="upload">分片上传。</param>
    /// <returns>得到的文件。</returns>
    public async Task<Result<SysFile>> CompleteAsync(InvocationContext context, SysFileUpload upload)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(upload);
        var cancellationToken = context.CancellationToken;
        var active = EnsureActive(upload);
        if (active.IsFailure)
        {
            return active.Error!;
        }

        var located = await LocateAsync(upload, cancellationToken).ConfigureAwait(false);
        if (!located.TryGetValue(out var target))
        {
            return located.Error!;
        }

        var multipart = MultipartOf(upload);
        var listed = await target.Multipart.ListPartsAsync(multipart, cancellationToken).ConfigureAwait(false);
        if (!listed.TryGetValue(out var uploaded))
        {
            return listed.Error!;
        }

        var parts = uploaded.Where(p => p.PartNumber <= upload.PartCount).OrderBy(static p => p.PartNumber).ToList();
        var present = parts.Select(static p => p.PartNumber).ToHashSet();
        var missing = Enumerable.Range(1, upload.PartCount).Where(n => !present.Contains(n)).ToList();
        if (missing.Count > 0)
        {
            return Error.Conflict(FilesErrors.UploadPartsIncomplete, FilesMessages.UploadPartsIncomplete, MessageArguments.Of(("missing", missing.Count), ("first", missing[0])));
        }

        var total = parts.Sum(static p => p.Length);
        if (total != upload.Size)
        {
            return Error.Conflict(FilesErrors.UploadSizeMismatch, FilesMessages.UploadSizeMismatch, MessageArguments.Of(("actual", total), ("expected", upload.Size)));
        }

        var allowed = await files.EnsureQuotaAsync(context, upload.TenantId, upload.Size).ConfigureAwait(false);
        if (allowed.IsFailure)
        {
            return allowed.Error!;
        }

        var completed = await target.Multipart.CompleteAsync(multipart, parts, cancellationToken).ConfigureAwait(false);
        if (completed.IsFailure)
        {
            return completed.Error!;
        }

        var opened = await target.Store.GetAsync(upload.Bucket, upload.Key, cancellationToken).ConfigureAwait(false);
        if (!opened.TryGetValue(out var blob))
        {
            return opened.Error!;
        }

        string hash;
        long length;
        await using (blob.ConfigureAwait(false))
        {
            (hash, length) = await FileService.DigestAsync(blob.Content, cancellationToken).ConfigureAwait(false);
        }

        if (length != upload.Size)
        {
            // 存储报的分片长度与拼出的不一致：对象不留，这次上传在存储里已经结束，只能重新发起
            await target.Store.DeleteAsync(upload.Bucket, upload.Key, cancellationToken).ConfigureAwait(false);
            return Error.Conflict(FilesErrors.UploadSizeMismatch, FilesMessages.UploadSizeMismatch, MessageArguments.Of(("actual", length), ("expected", upload.Size)));
        }

        var file = await files.AdoptAsync(upload, target.Store, hash, cancellationToken).ConfigureAwait(false);
        upload.FileId = file.Id;
        End(upload, FileUploadStatus.Completed);
        await rows.UpdateAsync(upload, cancellationToken).ConfigureAwait(false);
        return file;
    }

    /// <summary>
    /// 放弃分片上传。
    /// </summary>
    /// <remarks>
    /// 丢弃存储里已上传的分片（存储那边已经没有也算成功）；已结束的不能放弃。
    /// </remarks>
    /// <param name="upload">分片上传。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>放弃后的分片上传。</returns>
    public async Task<Result<SysFileUpload>> AbortAsync(SysFileUpload upload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (upload.Status != FileUploadStatus.Uploading)
        {
            return Error.Conflict(FilesErrors.UploadNotActive, FilesMessages.UploadNotActive, MessageArguments.Of(("status", upload.Status)));
        }

        var located = await LocateAsync(upload, cancellationToken).ConfigureAwait(false);
        if (!located.TryGetValue(out var target))
        {
            return located.Error!;
        }

        var aborted = await target.Multipart.AbortAsync(MultipartOf(upload), cancellationToken).ConfigureAwait(false);
        if (aborted.IsFailure)
        {
            return aborted.Error!;
        }

        End(upload, FileUploadStatus.Aborted);
        await rows.UpdateAsync(upload, cancellationToken).ConfigureAwait(false);
        return upload;
    }

    /// <summary>
    /// 把一侧过了时限的上传记为过期，放弃存储里的分片。
    /// </summary>
    /// <remarks>
    /// 一次至多 <see cref="ExpireBatchSize"/> 条；存储里放弃不了（存储不可用、配置已删）只记警告，记录照样记为过期。
    /// </remarks>
    /// <param name="side">侧。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>这一批记为过期的条数。</returns>
    public async Task<int> ExpireAsync(string side, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var due = await rows.Query<SysFileUpload>(side).Where(u => u.Status == FileUploadStatus.Uploading && u.ExpiresAt < now).Take(ExpireBatchSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var upload in due)
        {
            var located = await LocateAsync(upload, cancellationToken).ConfigureAwait(false);
            var error = located.Error;
            if (located.TryGetValue(out var target))
            {
                error = (await target.Multipart.AbortAsync(MultipartOf(upload), cancellationToken).ConfigureAwait(false)).Error;
            }

            if (error is not null)
            {
                LogNotAborted(logger, upload.Id, error.Code);
            }

            End(upload, FileUploadStatus.Expired);
            await rows.UpdateAsync(upload, cancellationToken).ConfigureAwait(false);
        }

        return due.Count;
    }

    private Result EnsureActive(SysFileUpload upload)
    {
        var status = StatusOf(upload);
        return status == FileUploadStatus.Uploading
            ? Result.Success()
            : Error.Conflict(FilesErrors.UploadNotActive, FilesMessages.UploadNotActive, MessageArguments.Of(("status", status)));
    }

    private void End(SysFileUpload upload, FileUploadStatus status)
    {
        upload.Status = status;
        upload.EndedAt = clock.GetUtcNow();
    }

    private static BlobMultipartUpload MultipartOf(SysFileUpload upload) => new(upload.Bucket, upload.Key, upload.UploadId);

    /// <summary>
    /// 上传的存储配置、对象存储与它的分片上传接口。
    /// </summary>
    private async Task<Result<Located>> LocateAsync(SysFileUpload upload, CancellationToken cancellationToken)
    {
        if (!(await configs.FindForSideAsync(upload.StorageConfigId, upload.TenantId, cancellationToken).ConfigureAwait(false)).TryGetValue(out var config))
        {
            return Error.NotFound(FilesErrors.StorageConfigNotFound, FilesMessages.StorageConfigNotFound, MessageArguments.Of(("id", upload.StorageConfigId)));
        }

        var store = await stores.GetAsync(config, cancellationToken).ConfigureAwait(false);
        if (!store.TryGetValue(out var blobs))
        {
            return store.Error!;
        }

        return blobs is IBlobMultipartStore multipart
            ? new Located(config, blobs, multipart)
            : Error.Unavailable(FilesErrors.MultipartUnsupported, FilesMessages.MultipartUnsupported, MessageArguments.Of(("code", config.Code)));
    }

    private sealed record Located(SysStorageConfig Config, IBlobStore Store, IBlobMultipartStore Multipart);

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "The expired upload {UploadId} was not aborted in storage ({Code}); it is marked expired.")]
    private static partial void LogNotAborted(ILogger logger, Guid uploadId, string code);
}
