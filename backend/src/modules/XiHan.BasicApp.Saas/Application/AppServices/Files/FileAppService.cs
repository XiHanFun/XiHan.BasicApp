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

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;
using static XiHan.BasicApp.Saas.Application.AppServices.SaasCommandValidation;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 系统文件命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统文件")]
public sealed class FileAppService(
    IFileRepository fileRepository,
    IFileStorageRepository fileStorageRepository)
    : SaasApplicationService, IFileAppService
{
    private readonly IFileRepository _fileRepository = fileRepository;
    private readonly IFileStorageRepository _fileStorageRepository = fileStorageRepository;

    /// <summary>
    /// 创建系统文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Create)]
    public async Task<FileDetailDto> CreateFileAsync(FileCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateFileInput(input.FileType, input.FileSize, input.Width, input.Height, input.Duration, input.ThumbnailFileId, input.AccessLevel, input.RetentionDays, input.Status);
        var file = new SysFile();
        ApplyFileInput(file, input);
        var savedFile = await _fileRepository.AddAsync(file, cancellationToken);
        return FileApplicationMapper.ToDetailDto(savedFile);
    }

    /// <summary>
    /// 更新系统文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Update)]
    public async Task<FileDetailDto> UpdateFileAsync(FileUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统文件主键必须大于 0。");
        ValidateFileInput(input.FileType, input.FileSize, input.Width, input.Height, input.Duration, input.ThumbnailFileId, input.AccessLevel, input.RetentionDays, status: null);
        var file = await GetFileOrThrowAsync(input.BasicId, cancellationToken);
        ApplyFileInput(file, input);
        var savedFile = await _fileRepository.UpdateAsync(file, cancellationToken);
        return FileApplicationMapper.ToDetailDto(savedFile);
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
        var savedFile = await _fileRepository.UpdateAsync(file, cancellationToken);
        return FileApplicationMapper.ToDetailDto(savedFile);
    }

    /// <summary>
    /// 删除系统文件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Delete)]
    public async Task DeleteFileAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var file = await GetFileOrThrowAsync(id, cancellationToken);
        _ = await _fileStorageRepository.DeleteAsync(storage => storage.FileId == file.BasicId, cancellationToken);
        if (!await _fileRepository.DeleteAsync(file, cancellationToken))
        {
            throw new InvalidOperationException("系统文件删除失败。");
        }
    }

    /// <summary>
    /// 创建系统文件存储
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Create)]
    public async Task<FileStorageDetailDto> CreateFileStorageAsync(FileStorageCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateFileStorageInput(input.FileId, input.StorageType, input.StoragePath, input.CompressionRatio, input.UploadDuration, input.RetryCount, input.SyncSourceId, input.Status);
        _ = await GetFileOrThrowAsync(input.FileId, cancellationToken);
        if (input.IsPrimary)
        {
            await ClearPrimaryStorageAsync(input.FileId, excludeStorageId: null, cancellationToken);
        }

        var storage = new SysFileStorage();
        ApplyFileStorageInput(storage, input);
        var savedStorage = await _fileStorageRepository.AddAsync(storage, cancellationToken);
        return FileApplicationMapper.ToStorageDetailDto(savedStorage);
    }

    /// <summary>
    /// 更新系统文件存储
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Update)]
    public async Task<FileStorageDetailDto> UpdateFileStorageAsync(FileStorageUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(input.BasicId, "系统文件存储主键必须大于 0。");
        ValidateFileStorageInput(fileId: null, input.StorageType, input.StoragePath, input.CompressionRatio, input.UploadDuration, input.RetryCount, input.SyncSourceId, status: null);
        var storage = await GetFileStorageOrThrowAsync(input.BasicId, cancellationToken);
        if (input.IsPrimary)
        {
            await ClearPrimaryStorageAsync(storage.FileId, storage.BasicId, cancellationToken);
        }

        ApplyFileStorageInput(storage, input);
        var savedStorage = await _fileStorageRepository.UpdateAsync(storage, cancellationToken);
        return FileApplicationMapper.ToStorageDetailDto(savedStorage);
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
        storage.UploadFailureReason = Optional(input.UploadFailureReason, 1000, nameof(input.UploadFailureReason), "上传失败原因不能超过 1000 个字符。");
        storage.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。") ?? storage.Remark;
        var savedStorage = await _fileStorageRepository.UpdateAsync(storage, cancellationToken);
        return FileApplicationMapper.ToStorageDetailDto(savedStorage);
    }

    /// <summary>
    /// 删除系统文件存储
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.File.Delete)]
    public async Task DeleteFileStorageAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storage = await GetFileStorageOrThrowAsync(id, cancellationToken);
        if (storage.IsPrimary && await _fileStorageRepository.AnyAsync(item => item.FileId == storage.FileId && item.BasicId != storage.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("主存储存在其他副本时不能直接删除，请先切换主存储。");
        }

        if (!await _fileStorageRepository.DeleteAsync(storage, cancellationToken))
        {
            throw new InvalidOperationException("系统文件存储删除失败。");
        }
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

    private Task<bool> ClearPrimaryStorageAsync(long fileId, long? excludeStorageId, CancellationToken cancellationToken)
    {
        return excludeStorageId.HasValue
            ? _fileStorageRepository.UpdateAsync(
                storage => new SysFileStorage { IsPrimary = false },
                storage => storage.FileId == fileId && storage.BasicId != excludeStorageId.Value && storage.IsPrimary,
                cancellationToken)
            : _fileStorageRepository.UpdateAsync(
                storage => new SysFileStorage { IsPrimary = false },
                storage => storage.FileId == fileId && storage.IsPrimary,
                cancellationToken);
    }

    private static void ApplyFileInput(SysFile file, FileCreateDto input)
    {
        file.FileName = Required(input.FileName, 255, nameof(input.FileName), "文件名不能超过 255 个字符。");
        file.OriginalName = Required(input.OriginalName, 255, nameof(input.OriginalName), "原始文件名不能超过 255 个字符。");
        file.FileExtension = Optional(input.FileExtension, 20, nameof(input.FileExtension), "文件扩展名不能超过 20 个字符。");
        file.FileType = input.FileType;
        file.MimeType = Optional(input.MimeType, 100, nameof(input.MimeType), "MIME 类型不能超过 100 个字符。");
        file.FileSize = input.FileSize;
        file.FileHash = Optional(input.FileHash, 128, nameof(input.FileHash), "文件哈希不能超过 128 个字符。");
        file.HashAlgorithm = Optional(input.HashAlgorithm, 20, nameof(input.HashAlgorithm), "哈希算法不能超过 20 个字符。");
        file.Width = input.Width;
        file.Height = input.Height;
        file.Duration = input.Duration;
        file.ThumbnailFileId = input.ThumbnailFileId;
        file.UploadIp = Optional(input.UploadIp, 64, nameof(input.UploadIp), "上传 IP 不能超过 64 个字符。");
        file.UploadSource = Optional(input.UploadSource, 50, nameof(input.UploadSource), "上传来源不能超过 50 个字符。");
        file.AccessLevel = input.AccessLevel;
        file.AccessPermissions = Optional(input.AccessPermissions, 1000, nameof(input.AccessPermissions), "访问权限不能超过 1000 个字符。");
        file.IsEncrypted = input.IsEncrypted;
        file.EncryptionKeyId = Optional(input.EncryptionKeyId, 100, nameof(input.EncryptionKeyId), "加密密钥主键不能超过 100 个字符。");
        file.ExpiresAt = input.ExpiresAt;
        file.IsTemporary = input.IsTemporary;
        file.RetentionDays = input.RetentionDays;
        file.Status = input.Status;
        file.Tags = Optional(input.Tags, 500, nameof(input.Tags), "文件标签不能超过 500 个字符。");
        file.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
        file.ExtendData = OptionalJson(input.ExtendData, "文件扩展数据必须是合法 JSON。");
    }

    private static void ApplyFileInput(SysFile file, FileUpdateDto input)
    {
        file.FileName = Required(input.FileName, 255, nameof(input.FileName), "文件名不能超过 255 个字符。");
        file.OriginalName = Required(input.OriginalName, 255, nameof(input.OriginalName), "原始文件名不能超过 255 个字符。");
        file.FileExtension = Optional(input.FileExtension, 20, nameof(input.FileExtension), "文件扩展名不能超过 20 个字符。");
        file.FileType = input.FileType;
        file.MimeType = Optional(input.MimeType, 100, nameof(input.MimeType), "MIME 类型不能超过 100 个字符。");
        file.FileSize = input.FileSize;
        file.FileHash = Optional(input.FileHash, 128, nameof(input.FileHash), "文件哈希不能超过 128 个字符。");
        file.HashAlgorithm = Optional(input.HashAlgorithm, 20, nameof(input.HashAlgorithm), "哈希算法不能超过 20 个字符。");
        file.Width = input.Width;
        file.Height = input.Height;
        file.Duration = input.Duration;
        file.ThumbnailFileId = input.ThumbnailFileId;
        file.AccessLevel = input.AccessLevel;
        file.AccessPermissions = Optional(input.AccessPermissions, 1000, nameof(input.AccessPermissions), "访问权限不能超过 1000 个字符。");
        file.IsEncrypted = input.IsEncrypted;
        file.EncryptionKeyId = Optional(input.EncryptionKeyId, 100, nameof(input.EncryptionKeyId), "加密密钥主键不能超过 100 个字符。");
        file.ExpiresAt = input.ExpiresAt;
        file.IsTemporary = input.IsTemporary;
        file.RetentionDays = input.RetentionDays;
        file.Tags = Optional(input.Tags, 500, nameof(input.Tags), "文件标签不能超过 500 个字符。");
        file.Remark = Optional(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
        file.ExtendData = OptionalJson(input.ExtendData, "文件扩展数据必须是合法 JSON。");
    }

    private static void ApplyFileStorageInput(SysFileStorage storage, FileStorageCreateDto input)
    {
        storage.FileId = input.FileId;
        ApplyFileStorageInputCore(storage, input.StorageType, input.StorageProvider, input.StorageConfigId, input.StorageRegion, input.BucketName, input.StoragePath, input.FullPath, input.InternalUrl, input.ExternalUrl, input.CdnUrl, input.IsPrimary, input.IsBackup, input.EnableCdn, input.IsCompressed, input.CompressionRatio, input.UploadedAt, input.UploadDuration, input.UploadFailureReason, input.RetryCount, input.LastVerifiedAt, input.IsVerified, input.IsSynced, input.SyncedAt, input.SyncSourceId, input.AccessControl, input.StorageClass, input.CacheControl, input.SortOrder, input.Remark, input.ExtendData);
        storage.Status = input.Status;
    }

    private static void ApplyFileStorageInput(SysFileStorage storage, FileStorageUpdateDto input)
    {
        ApplyFileStorageInputCore(storage, input.StorageType, input.StorageProvider, input.StorageConfigId, input.StorageRegion, input.BucketName, input.StoragePath, input.FullPath, input.InternalUrl, input.ExternalUrl, input.CdnUrl, input.IsPrimary, input.IsBackup, input.EnableCdn, input.IsCompressed, input.CompressionRatio, input.UploadedAt, input.UploadDuration, input.UploadFailureReason, input.RetryCount, input.LastVerifiedAt, input.IsVerified, input.IsSynced, input.SyncedAt, input.SyncSourceId, input.AccessControl, input.StorageClass, input.CacheControl, input.SortOrder, input.Remark, input.ExtendData);
    }

    private static void ApplyFileStorageInputCore(
        SysFileStorage storage,
        FileStorageType storageType,
        string? storageProvider,
        long? storageConfigId,
        string? storageRegion,
        string? bucketName,
        string storagePath,
        string? fullPath,
        string? internalUrl,
        string? externalUrl,
        string? cdnUrl,
        bool isPrimary,
        bool isBackup,
        bool enableCdn,
        bool isCompressed,
        decimal? compressionRatio,
        DateTimeOffset? uploadedAt,
        long? uploadDuration,
        string? uploadFailureReason,
        int retryCount,
        DateTimeOffset? lastVerifiedAt,
        bool isVerified,
        bool isSynced,
        DateTimeOffset? syncedAt,
        long? syncSourceId,
        string? accessControl,
        string? storageClass,
        string? cacheControl,
        int sortOrder,
        string? remark,
        string? extendData)
    {
        storage.StorageType = storageType;
        storage.StorageProvider = Optional(storageProvider, 100, nameof(storageProvider), "存储服务商不能超过 100 个字符。");
        storage.StorageConfigId = storageConfigId;
        storage.StorageRegion = Optional(storageRegion, 100, nameof(storageRegion), "存储区域不能超过 100 个字符。");
        storage.BucketName = Optional(bucketName, 200, nameof(bucketName), "存储桶不能超过 200 个字符。");
        storage.StoragePath = Required(storagePath, 1000, nameof(storagePath), "存储路径不能超过 1000 个字符。");
        storage.FullPath = Optional(fullPath, 2000, nameof(fullPath), "完整路径不能超过 2000 个字符。");
        storage.InternalUrl = Optional(internalUrl, 2000, nameof(internalUrl), "内部地址不能超过 2000 个字符。");
        storage.ExternalUrl = Optional(externalUrl, 2000, nameof(externalUrl), "外部地址不能超过 2000 个字符。");
        storage.CdnUrl = Optional(cdnUrl, 2000, nameof(cdnUrl), "CDN 地址不能超过 2000 个字符。");
        storage.IsPrimary = isPrimary;
        storage.IsBackup = isBackup;
        storage.EnableCdn = enableCdn;
        storage.IsCompressed = isCompressed;
        storage.CompressionRatio = compressionRatio;
        storage.UploadedAt = uploadedAt;
        storage.UploadDuration = uploadDuration;
        storage.UploadFailureReason = Optional(uploadFailureReason, 1000, nameof(uploadFailureReason), "上传失败原因不能超过 1000 个字符。");
        storage.RetryCount = retryCount;
        storage.LastVerifiedAt = lastVerifiedAt;
        storage.IsVerified = isVerified;
        storage.IsSynced = isSynced;
        storage.SyncedAt = syncedAt;
        storage.SyncSourceId = syncSourceId;
        storage.AccessControl = Optional(accessControl, 100, nameof(accessControl), "访问控制不能超过 100 个字符。");
        storage.StorageClass = Optional(storageClass, 100, nameof(storageClass), "存储类型不能超过 100 个字符。");
        storage.CacheControl = Optional(cacheControl, 200, nameof(cacheControl), "缓存控制不能超过 200 个字符。");
        storage.SortOrder = sortOrder;
        storage.Remark = Optional(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        storage.ExtendData = OptionalJson(extendData, "文件存储扩展数据必须是合法 JSON。");
    }

    private static void ValidateFileInput(FileType fileType, long fileSize, int? width, int? height, int? duration, long? thumbnailFileId, ResourceAccessLevel accessLevel, int retentionDays, FileStatus? status)
    {
        EnsureEnum(fileType, nameof(fileType));
        EnsureEnum(accessLevel, nameof(accessLevel));
        if (status.HasValue)
        {
            EnsureEnum(status.Value, nameof(status));
        }

        EnsureNonNegative(fileSize, nameof(fileSize), "文件大小不能小于 0。");
        EnsureOptionalNonNegative(width, nameof(width), "图片宽度不能小于 0。");
        EnsureOptionalNonNegative(height, nameof(height), "图片高度不能小于 0。");
        EnsureOptionalNonNegative(duration, nameof(duration), "媒体时长不能小于 0。");
        EnsureOptionalId(thumbnailFileId, nameof(thumbnailFileId), "缩略图文件主键必须大于 0。");
        EnsureNonNegative(retentionDays, nameof(retentionDays), "保留天数不能小于 0。");
    }

    private static void ValidateFileStorageInput(long? fileId, FileStorageType storageType, string storagePath, decimal? compressionRatio, long? uploadDuration, int retryCount, long? syncSourceId, FileStorageStatus? status)
    {
        EnsureOptionalId(fileId, nameof(fileId), "系统文件主键必须大于 0。");
        EnsureEnum(storageType, nameof(storageType));
        if (status.HasValue)
        {
            EnsureEnum(status.Value, nameof(status));
        }

        _ = Required(storagePath, 1000, nameof(storagePath), "存储路径不能超过 1000 个字符。");
        if (compressionRatio is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(compressionRatio), "压缩率不能小于 0。");
        }

        EnsureOptionalNonNegative(uploadDuration, nameof(uploadDuration), "上传耗时不能小于 0。");
        EnsureNonNegative(retryCount, nameof(retryCount), "重试次数不能小于 0。");
        EnsureOptionalId(syncSourceId, nameof(syncSourceId), "同步来源主键必须大于 0。");
    }
}
