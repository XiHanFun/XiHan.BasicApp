#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileAppService
// Guid:0de54b67-1c8c-4b25-af1c-b010c60f2091
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.ObjectStorage;
using XiHan.Framework.ObjectStorage.Models;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Web.Core.Clients;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统文件命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统文件")]
public sealed class FileAppService(
    IFileRepository fileRepository,
    IFileStorageRepository fileStorageRepository,
    IFileStorageRouter fileStorageRouter,
    IFileStorageProviderManager fileStorageProviderManager,
    IFileStorageDomainService fileStorageDomainService,
    IClientInfoProvider clientInfoProvider,
    ILocalEventBus localEventBus,
    ICurrentUser currentUser)
    : SaasApplicationService, IFileAppService
{
    private const string Sha256AlgorithmName = "SHA256";

    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IFileStorageRepository _fileStorageRepository = fileStorageRepository;
    private readonly IFileStorageRouter _fileStorageRouter = fileStorageRouter;
    private readonly IFileStorageProviderManager _fileStorageProviderManager = fileStorageProviderManager;
    private readonly IFileStorageDomainService _fileStorageDomainService = fileStorageDomainService;
    private readonly IClientInfoProvider _clientInfoProvider = clientInfoProvider;
    private readonly ILocalEventBus _localEventBus = localEventBus;
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 上传文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Create)]
    public async Task<FileDetailDto> UploadFileAsync(FileUploadDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.File);
        cancellationToken.ThrowIfCancellationRequested();

        _fileStorageDomainService.EnsureUploadMetadata(input.File.Length, input.IsTemporary, input.ExpiresAt, input.RetentionDays);
        ValidateMutableMetadata(input.AccessLevel, input.RetentionDays, input.Width, input.Height, input.Duration, input.ThumbnailFileId);

        var originalName = _fileStorageDomainService.NormalizeOriginalName(input.File.FileName);
        var fileHash = await ComputeSha256Async(input.File, cancellationToken);
        var existing = await _fileRepository.GetByHashAsync(fileHash, cancellationToken);
        if (existing is { Status: FileStatus.Normal })
        {
            return FileApplicationMapper.ToDetailDto(existing);
        }

        var now = DateTimeOffset.UtcNow;
        var resolvedProviderName = _fileStorageRouter.ResolveProviderName(input.RouteKey, input.ProviderName);
        var provider = _fileStorageRouter.Route(input.RouteKey, resolvedProviderName);
        var storedFileName = _fileStorageDomainService.BuildStoredFileName(originalName, fileHash);
        var storagePath = _fileStorageDomainService.BuildStoragePath(storedFileName, input.Directory, now);
        var accessControl = _fileStorageDomainService.ResolveAccessControl(input.AccessLevel, input.AccessControl);
        var clientInfo = _clientInfoProvider.GetCurrent();

        var file = new SysFile
        {
            FileName = storedFileName,
            OriginalName = originalName,
            FileExtension = Path.GetExtension(originalName),
            FileType = _fileStorageDomainService.ResolveFileType(Path.GetExtension(originalName), input.File.ContentType),
            MimeType = NormalizeText(input.File.ContentType, 100),
            FileSize = input.File.Length,
            FileHash = fileHash,
            HashAlgorithm = Sha256AlgorithmName,
            Width = input.Width,
            Height = input.Height,
            Duration = input.Duration,
            ThumbnailFileId = input.ThumbnailFileId,
            UploadIp = NormalizeText(clientInfo.IpAddress, 50),
            UploadSource = ResolveUploadSource(clientInfo),
            AccessLevel = input.AccessLevel,
            AccessPermissions = Optional(input.AccessPermissions, 500, nameof(input.AccessPermissions), "访问权限不能超过 500 个字符。"),
            IsEncrypted = input.IsEncrypted,
            EncryptionKeyId = Optional(input.EncryptionKeyId, 100, nameof(input.EncryptionKeyId), "加密密钥主键不能超过 100 个字符。"),
            ExpiresAt = input.ExpiresAt,
            IsTemporary = input.IsTemporary,
            RetentionDays = input.RetentionDays,
            Status = FileStatus.Uploading,
            Tags = Optional(input.Tags, 500, nameof(input.Tags), "文件标签不能超过 500 个字符。"),
            Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。"),
            ExtendData = OptionalJson(input.ExtendData, "文件扩展数据必须是合法 JSON。")
        };

        file = await _fileRepository.AddAsync(file, cancellationToken);

        FileUploadResult uploadResult;
        await using (var stream = input.File.OpenReadStream())
        {
            uploadResult = await provider.UploadAsync(
                new FileUploadRequest
                {
                    FileStream = stream,
                    FileName = storedFileName,
                    StoragePath = storagePath,
                    ContentType = input.File.ContentType,
                    BucketName = NormalizeText(input.BucketName, 100),
                    Overwrite = input.Overwrite,
                    AccessControl = accessControl,
                    CacheControl = NormalizeText(input.CacheControl, 100),
                    Metadata = new Dictionary<string, string>
                    {
                        ["file-id"] = file.BasicId.ToString(),
                        ["file-hash"] = fileHash,
                        ["hash-algorithm"] = Sha256AlgorithmName
                    }
                },
                cancellationToken);
        }

        if (!uploadResult.Success)
        {
            file.Status = FileStatus.UploadFailed;
            file.Remark = NormalizeText(uploadResult.ErrorMessage, 500) ?? file.Remark;
            _ = await _fileRepository.UpdateAsync(file, cancellationToken);
            throw new InvalidOperationException(uploadResult.ErrorMessage ?? "文件上传失败。");
        }

        file.FileSize = uploadResult.FileSize > 0 ? uploadResult.FileSize : file.FileSize;
        file.Status = FileStatus.Normal;
        file = await _fileRepository.UpdateAsync(file, cancellationToken);

        var storage = new SysFileStorage
        {
            FileId = file.BasicId,
            StorageType = _fileStorageDomainService.ResolveStorageType(provider.ProviderName),
            StorageProvider = NormalizeText(provider.ProviderName, 50),
            BucketName = NormalizeText(input.BucketName, 100),
            StoragePath = uploadResult.Path ?? storagePath,
            FullPath = NormalizeText(uploadResult.FullPath, 1000),
            InternalUrl = NormalizeText(uploadResult.Url, 1000),
            ExternalUrl = NormalizeText(uploadResult.Url, 1000),
            IsPrimary = true,
            IsBackup = false,
            Status = FileStorageStatus.Normal,
            UploadedAt = now,
            UploadDuration = uploadResult.DurationMs,
            LastVerifiedAt = now,
            IsVerified = true,
            IsSynced = true,
            SyncedAt = now,
            AccessControl = accessControl,
            CacheControl = NormalizeText(input.CacheControl, 100),
            SortOrder = 0,
            Remark = NormalizeText(input.Remark, 500)
        };

        await _fileStorageRepository.ClearPrimaryAsync(file.BasicId, excludeStorageId: null, cancellationToken);
        storage = await _fileStorageRepository.AddAsync(storage, cancellationToken);
        await PublishUploadedAsync(file, storage, input.Remark, cancellationToken);
        return FileApplicationMapper.ToDetailDto(file);
    }

    /// <summary>
    /// 秒传文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Create)]
    public async Task<FileDetailDto> FastUploadFileAsync(FileFastUploadDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var fileHash = Required(input.FileHash, 100, nameof(input.FileHash), "文件哈希不能超过 100 个字符。");
        _fileStorageDomainService.EnsureUploadMetadata(input.FileSize, input.IsTemporary, input.ExpiresAt, input.RetentionDays);

        var existing = await _fileRepository.GetByHashAsync(fileHash, cancellationToken)
            ?? throw new InvalidOperationException("文件不存在，无法秒传。");
        if (existing.Status != FileStatus.Normal)
        {
            throw new InvalidOperationException("文件状态非正常，无法秒传。");
        }

        var primaryStorage = await _fileStorageRepository.GetPrimaryByFileIdAsync(existing.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("文件缺少可用主存储，无法秒传。");
        if (primaryStorage.Status != FileStorageStatus.Normal)
        {
            throw new InvalidOperationException("文件主存储不可用，无法秒传。");
        }

        ApplyFastUploadMetadata(existing, input);
        existing = await _fileRepository.UpdateAsync(existing, cancellationToken);
        return FileApplicationMapper.ToDetailDto(existing);
    }

    /// <summary>
    /// 更新文件业务元数据
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Update)]
    public async Task<FileDetailDto> UpdateFileMetadataAsync(FileMetadataUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统文件主键必须大于 0。");
        ValidateMutableMetadata(input.AccessLevel, input.RetentionDays, input.Width, input.Height, input.Duration, input.ThumbnailFileId);
        _fileStorageDomainService.EnsureUploadMetadata(1, input.IsTemporary, input.ExpiresAt, input.RetentionDays);

        var file = await GetFileOrThrowAsync(input.BasicId, cancellationToken);
        ApplyMutableMetadata(file, input);
        file = await _fileRepository.UpdateAsync(file, cancellationToken);
        return FileApplicationMapper.ToDetailDto(file);
    }

    /// <summary>
    /// 更新系统文件状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Status)]
    public async Task<FileDetailDto> UpdateFileStatusAsync(FileStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统文件主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));
        var file = await GetFileOrThrowAsync(input.BasicId, cancellationToken);
        file.Status = input.Status;
        file.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? file.Remark;
        file = await _fileRepository.UpdateAsync(file, cancellationToken);
        return FileApplicationMapper.ToDetailDto(file);
    }

    /// <summary>
    /// 切换主存储
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Update)]
    public async Task<FileStorageDetailDto> SwitchPrimaryStorageAsync(FilePrimaryStorageSwitchDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统文件主键必须大于 0。");
        EnsureId(input.StorageId, "系统文件存储主键必须大于 0。");

        _ = await GetFileOrThrowAsync(input.BasicId, cancellationToken);
        var storage = await GetFileStorageOrThrowAsync(input.StorageId, cancellationToken);
        if (storage.FileId != input.BasicId)
        {
            throw new InvalidOperationException("存储副本不属于当前文件。");
        }

        _fileStorageDomainService.EnsureCanBecomePrimary(storage);
        var previousStorage = await _fileStorageRepository.GetPrimaryByFileIdAsync(input.BasicId, cancellationToken);
        await _fileStorageRepository.ClearPrimaryAsync(input.BasicId, storage.BasicId, cancellationToken);
        storage.IsPrimary = true;
        storage.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? storage.Remark;
        storage = await _fileStorageRepository.UpdateAsync(storage, cancellationToken);

        await _localEventBus.PublishAsync(
            new FilePrimaryStorageChangedDomainEvent(
                storage.TenantId,
                storage.FileId,
                storage.BasicId,
                previousStorage?.BasicId,
                _currentUser.UserId,
                input.Remark));

        return FileApplicationMapper.ToStorageDetailDto(storage);
    }

    /// <summary>
    /// 校验文件存储副本
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Status)]
    public async Task<FileStorageDetailDto> VerifyFileStorageAsync(FileStorageVerifyDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统文件存储主键必须大于 0。");
        var storage = await GetFileStorageOrThrowAsync(input.BasicId, cancellationToken);
        var file = await GetFileOrThrowAsync(storage.FileId, cancellationToken);
        var provider = _fileStorageRouter.Route(providerName: storage.StorageProvider);
        var exists = await provider.ExistsAsync(storage.StoragePath, storage.BucketName, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        storage.LastVerifiedAt = now;
        storage.IsVerified = exists;
        storage.Status = exists ? FileStorageStatus.Normal : FileStorageStatus.VerificationFailed;
        storage.UploadFailureReason = exists ? null : "对象存储文件不存在。";
        storage.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? storage.Remark;

        if (exists)
        {
            var metadata = await provider.GetMetadataAsync(storage.StoragePath, storage.BucketName, cancellationToken);
            storage.ExternalUrl = NormalizeText(metadata.Url, 1000) ?? storage.ExternalUrl;
            file.FileSize = metadata.Size > 0 ? metadata.Size : file.FileSize;
            if (file.Status is FileStatus.UploadFailed or FileStatus.Corrupted)
            {
                file.Status = FileStatus.Normal;
            }
        }
        else if (storage.IsPrimary)
        {
            file.Status = FileStatus.Corrupted;
        }

        storage = await _fileStorageRepository.UpdateAsync(storage, cancellationToken);
        _ = await _fileRepository.UpdateAsync(file, cancellationToken);
        return FileApplicationMapper.ToStorageDetailDto(storage);
    }

    /// <summary>
    /// 更新系统文件存储状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Status)]
    public async Task<FileStorageDetailDto> UpdateFileStorageStatusAsync(FileStorageStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统文件存储主键必须大于 0。");
        EnsureEnum(input.Status, nameof(input.Status));
        var storage = await GetFileStorageOrThrowAsync(input.BasicId, cancellationToken);
        storage.Status = input.Status;
        storage.UploadFailureReason = Optional(input.UploadFailureReason, 500, nameof(input.UploadFailureReason), "上传失败原因不能超过 500 个字符。");
        storage.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? storage.Remark;
        storage = await _fileStorageRepository.UpdateAsync(storage, cancellationToken);
        return FileApplicationMapper.ToStorageDetailDto(storage);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Delete)]
    public async Task DeleteFileAsync(FileDeleteDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统文件主键必须大于 0。");
        var file = await GetFileOrThrowAsync(input.BasicId, cancellationToken);
        var storages = await _fileStorageRepository.GetByFileIdAsync(file.BasicId, cancellationToken);

        if (input.DeletePhysical)
        {
            foreach (var storage in storages)
            {
                var provider = _fileStorageRouter.Route(providerName: storage.StorageProvider);
                await provider.DeleteAsync(storage.StoragePath, storage.BucketName, cancellationToken);
            }
        }

        file.Status = FileStatus.Deleted;
        file.Remark = Optional(input.Reason, 500, nameof(input.Reason), "删除原因不能超过 500 个字符。") ?? file.Remark;
        _ = await _fileRepository.UpdateAsync(file, cancellationToken);

        foreach (var storage in storages)
        {
            storage.Status = FileStorageStatus.Deleted;
            storage.Remark = file.Remark;
        }

        if (storages.Count > 0)
        {
            _ = await _fileStorageRepository.UpdateRangeAsync(storages, cancellationToken);
            _ = await _fileStorageRepository.DeleteAsync(storage => storage.FileId == file.BasicId, cancellationToken);
        }

        if (!await _fileRepository.DeleteAsync(file, cancellationToken))
        {
            throw new InvalidOperationException("系统文件删除失败。");
        }

        await _localEventBus.PublishAsync(
            new FileDeletedDomainEvent(
                file.TenantId,
                file.BasicId,
                file.FileName,
                input.DeletePhysical,
                _currentUser.UserId,
                input.Reason));
    }

    /// <summary>
    /// 下载文件
    /// </summary>
    [UnitOfWork]
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    public async Task<Stream> DownloadFileAsync(long fileId, CancellationToken cancellationToken = default)
    {
        EnsureId(fileId, "系统文件主键必须大于 0。");
        cancellationToken.ThrowIfCancellationRequested();

        var file = await GetFileOrThrowAsync(fileId, cancellationToken);
        var storage = await _fileStorageRepository.GetPrimaryByFileIdAsync(file.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("文件缺少可用主存储，无法下载。");

        var provider = _fileStorageRouter.Route(providerName: storage.StorageProvider);
        return await provider.DownloadAsync(storage.StoragePath, cancellationToken);
    }

    /// <summary>
    /// 生成文件预签名访问 URL
    /// </summary>
    [UnitOfWork]
    [PermissionAuthorize(SaasPermissionCodes.File.Read)]
    public async Task<string> GenerateFilePresignedUrlAsync(long fileId, TimeSpan? expiresIn = null, CancellationToken cancellationToken = default)
    {
        EnsureId(fileId, "系统文件主键必须大于 0。");
        cancellationToken.ThrowIfCancellationRequested();

        var file = await GetFileOrThrowAsync(fileId, cancellationToken);
        var storage = await _fileStorageRepository.GetPrimaryByFileIdAsync(file.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("文件缺少可用主存储，无法生成访问链接。");

        var provider = _fileStorageRouter.Route(providerName: storage.StorageProvider);
        var effectiveExpiresIn = expiresIn ?? TimeSpan.FromMinutes(30);
        return await provider.GeneratePresignedUrlAsync(storage.StoragePath, effectiveExpiresIn, cancellationToken);
    }

    private async Task<SysFile> GetFileOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统文件主键必须大于 0。");
        return await _fileRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统文件不存在。");
    }

    private async Task<SysFileStorage> GetFileStorageOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "系统文件存储主键必须大于 0。");
        return await _fileStorageRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("系统文件存储不存在。");
    }

    private async Task PublishUploadedAsync(SysFile file, SysFileStorage storage, string? reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _localEventBus.PublishAsync(
            new FileUploadedDomainEvent(
                file.TenantId,
                file.BasicId,
                storage.BasicId,
                file.FileName,
                file.FileSize,
                _currentUser.UserId,
                reason));
    }

    private static async Task<string> ComputeSha256Async(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void ApplyFastUploadMetadata(SysFile file, FileFastUploadDto input)
    {
        file.OriginalName = Required(input.OriginalName, 200, nameof(input.OriginalName), "原始文件名不能超过 200 个字符。");
        file.FileExtension = Optional(input.FileExtension, 20, nameof(input.FileExtension), "文件扩展名不能超过 20 个字符。") ?? file.FileExtension;
        file.MimeType = Optional(input.MimeType, 100, nameof(input.MimeType), "MIME 类型不能超过 100 个字符。") ?? file.MimeType;
        file.AccessLevel = input.AccessLevel;
        file.AccessPermissions = Optional(input.AccessPermissions, 500, nameof(input.AccessPermissions), "访问权限不能超过 500 个字符。");
        file.ExpiresAt = input.ExpiresAt;
        file.IsTemporary = input.IsTemporary;
        file.RetentionDays = input.RetentionDays;
        file.Tags = Optional(input.Tags, 500, nameof(input.Tags), "文件标签不能超过 500 个字符。") ?? file.Tags;
        file.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? file.Remark;
        file.ExtendData = OptionalJson(input.ExtendData, "文件扩展数据必须是合法 JSON。") ?? file.ExtendData;
    }

    private static void ApplyMutableMetadata(SysFile file, FileMetadataUpdateDto input)
    {
        file.Width = input.Width;
        file.Height = input.Height;
        file.Duration = input.Duration;
        file.ThumbnailFileId = input.ThumbnailFileId;
        file.AccessLevel = input.AccessLevel;
        file.AccessPermissions = Optional(input.AccessPermissions, 500, nameof(input.AccessPermissions), "访问权限不能超过 500 个字符。");
        file.IsEncrypted = input.IsEncrypted;
        file.EncryptionKeyId = Optional(input.EncryptionKeyId, 100, nameof(input.EncryptionKeyId), "加密密钥主键不能超过 100 个字符。");
        file.ExpiresAt = input.ExpiresAt;
        file.IsTemporary = input.IsTemporary;
        file.RetentionDays = input.RetentionDays;
        file.Tags = Optional(input.Tags, 500, nameof(input.Tags), "文件标签不能超过 500 个字符。");
        file.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
        file.ExtendData = OptionalJson(input.ExtendData, "文件扩展数据必须是合法 JSON。");
    }

    private static void ValidateMutableMetadata(ResourceAccessLevel accessLevel, int retentionDays, int? width, int? height, int? duration, long? thumbnailFileId)
    {
        EnsureEnum(accessLevel, nameof(accessLevel));
        EnsureNonNegative(retentionDays, nameof(retentionDays), "保留天数不能小于 0。");
        EnsureOptionalNonNegative(width, nameof(width), "图片宽度不能小于 0。");
        EnsureOptionalNonNegative(height, nameof(height), "图片高度不能小于 0。");
        EnsureOptionalNonNegative(duration, nameof(duration), "媒体时长不能小于 0。");
        EnsureOptionalId(thumbnailFileId, nameof(thumbnailFileId), "缩略图文件主键必须大于 0。");
    }

    private static string? ResolveUploadSource(ClientInfo clientInfo)
    {
        var parts = new[]
        {
            NormalizeText(clientInfo.DeviceName, 20),
            NormalizeText(clientInfo.OperatingSystem, 20),
            NormalizeText(clientInfo.Browser, 20)
        }.Where(part => !string.IsNullOrWhiteSpace(part));

        return NormalizeText(string.Join("/", parts), 50);
    }

    private static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
