#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileApplicationMapper
// Guid:7c32015f-28b6-4e93-9ae3-42d80edf2299
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统文件应用层映射器
/// </summary>
public static class FileApplicationMapper
{
    /// <summary>
    /// 映射系统文件列表项
    /// </summary>
    /// <param name="file">系统文件实体</param>
    /// <returns>系统文件列表项 DTO</returns>
    public static FileListItemDto ToListItemDto(SysFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return new FileListItemDto
        {
            BasicId = file.BasicId,
            FileName = file.FileName,
            OriginalName = file.OriginalName,
            FileExtension = file.FileExtension,
            FileType = file.FileType,
            MimeType = file.MimeType,
            FileSize = file.FileSize,
            Width = file.Width,
            Height = file.Height,
            Duration = file.Duration,
            ThumbnailFileId = file.ThumbnailFileId,
            DownloadCount = file.DownloadCount,
            ViewCount = file.ViewCount,
            LastDownloadTime = file.LastDownloadTime,
            LastAccessTime = file.LastAccessTime,
            AccessLevel = file.AccessLevel,
            IsEncrypted = file.IsEncrypted,
            ExpiresAt = file.ExpiresAt,
            IsTemporary = file.IsTemporary,
            RetentionDays = file.RetentionDays,
            Status = file.Status,
            HasIntegrityFingerprint = !string.IsNullOrWhiteSpace(file.FileHash),
            HasAccessRule = !string.IsNullOrWhiteSpace(file.AccessPermissions),
            HasLabels = !string.IsNullOrWhiteSpace(file.Tags),
            HasNote = !string.IsNullOrWhiteSpace(file.Remark),
            HasExtension = !string.IsNullOrWhiteSpace(file.ExtendData),
            CreatedTime = file.CreatedTime,
            ModifiedTime = file.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统文件详情
    /// </summary>
    /// <param name="file">系统文件实体</param>
    /// <returns>系统文件详情 DTO</returns>
    public static FileDetailDto ToDetailDto(SysFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var item = ToListItemDto(file);
        return new FileDetailDto
        {
            BasicId = item.BasicId,
            FileName = item.FileName,
            OriginalName = item.OriginalName,
            FileExtension = item.FileExtension,
            FileType = item.FileType,
            MimeType = item.MimeType,
            FileSize = item.FileSize,
            Width = item.Width,
            Height = item.Height,
            Duration = item.Duration,
            ThumbnailFileId = item.ThumbnailFileId,
            DownloadCount = item.DownloadCount,
            ViewCount = item.ViewCount,
            LastDownloadTime = item.LastDownloadTime,
            LastAccessTime = item.LastAccessTime,
            AccessLevel = item.AccessLevel,
            IsEncrypted = item.IsEncrypted,
            ExpiresAt = item.ExpiresAt,
            IsTemporary = item.IsTemporary,
            RetentionDays = item.RetentionDays,
            Status = item.Status,
            HasIntegrityFingerprint = item.HasIntegrityFingerprint,
            HasAccessRule = item.HasAccessRule,
            HasLabels = item.HasLabels,
            HasNote = item.HasNote,
            HasExtension = item.HasExtension,
            CreatedTime = item.CreatedTime,
            CreatedId = file.CreatedId,
            CreatedBy = file.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = file.ModifiedId,
            ModifiedBy = file.ModifiedBy
        };
    }

    /// <summary>
    /// 映射系统文件存储列表项
    /// </summary>
    /// <param name="storage">系统文件存储实体</param>
    /// <returns>系统文件存储列表项 DTO</returns>
    public static FileStorageListItemDto ToStorageListItemDto(SysFileStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);

        return new FileStorageListItemDto
        {
            BasicId = storage.BasicId,
            FileId = storage.FileId,
            StorageType = storage.StorageType,
            StorageProvider = storage.StorageProvider,
            StorageRegion = storage.StorageRegion,
            IsPrimary = storage.IsPrimary,
            IsBackup = storage.IsBackup,
            EnableCdn = storage.EnableCdn,
            IsCompressed = storage.IsCompressed,
            CompressionRatio = storage.CompressionRatio,
            Status = storage.Status,
            UploadedAt = storage.UploadedAt,
            UploadDuration = storage.UploadDuration,
            RetryCount = storage.RetryCount,
            LastVerifiedAt = storage.LastVerifiedAt,
            IsVerified = storage.IsVerified,
            IsSynced = storage.IsSynced,
            SyncedAt = storage.SyncedAt,
            SyncSourceId = storage.SyncSourceId,
            AccessControl = storage.AccessControl,
            StorageClass = storage.StorageClass,
            CacheControl = storage.CacheControl,
            SortOrder = storage.SortOrder,
            HasLocation = !string.IsNullOrWhiteSpace(storage.BucketName)
                || !string.IsNullOrWhiteSpace(storage.StoragePath)
                || !string.IsNullOrWhiteSpace(storage.FullPath)
                || !string.IsNullOrWhiteSpace(storage.StorageDirectory),
            HasPrivateLink = !string.IsNullOrWhiteSpace(storage.InternalUrl),
            HasPublicLink = !string.IsNullOrWhiteSpace(storage.ExternalUrl)
                || !string.IsNullOrWhiteSpace(storage.Endpoint)
                || !string.IsNullOrWhiteSpace(storage.CustomDomain),
            HasCdnLink = !string.IsNullOrWhiteSpace(storage.CdnUrl),
            HasTemporaryLink = !string.IsNullOrWhiteSpace(storage.SignedUrl),
            HasFailureDetail = !string.IsNullOrWhiteSpace(storage.UploadFailureReason),
            HasNote = !string.IsNullOrWhiteSpace(storage.Remark),
            HasExtension = !string.IsNullOrWhiteSpace(storage.ExtendData),
            CreatedTime = storage.CreatedTime,
            ModifiedTime = storage.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统文件存储详情
    /// </summary>
    /// <param name="storage">系统文件存储实体</param>
    /// <returns>系统文件存储详情 DTO</returns>
    public static FileStorageDetailDto ToStorageDetailDto(SysFileStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);

        var item = ToStorageListItemDto(storage);
        return new FileStorageDetailDto
        {
            BasicId = item.BasicId,
            FileId = item.FileId,
            StorageType = item.StorageType,
            StorageProvider = item.StorageProvider,
            StorageRegion = item.StorageRegion,
            IsPrimary = item.IsPrimary,
            IsBackup = item.IsBackup,
            EnableCdn = item.EnableCdn,
            IsCompressed = item.IsCompressed,
            CompressionRatio = item.CompressionRatio,
            Status = item.Status,
            UploadedAt = item.UploadedAt,
            UploadDuration = item.UploadDuration,
            RetryCount = item.RetryCount,
            LastVerifiedAt = item.LastVerifiedAt,
            IsVerified = item.IsVerified,
            IsSynced = item.IsSynced,
            SyncedAt = item.SyncedAt,
            SyncSourceId = item.SyncSourceId,
            AccessControl = item.AccessControl,
            StorageClass = item.StorageClass,
            CacheControl = item.CacheControl,
            SortOrder = item.SortOrder,
            HasLocation = item.HasLocation,
            HasPrivateLink = item.HasPrivateLink,
            HasPublicLink = item.HasPublicLink,
            HasCdnLink = item.HasCdnLink,
            HasTemporaryLink = item.HasTemporaryLink,
            HasFailureDetail = item.HasFailureDetail,
            HasNote = item.HasNote,
            HasExtension = item.HasExtension,
            CreatedTime = item.CreatedTime,
            CreatedId = storage.CreatedId,
            CreatedBy = storage.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = storage.ModifiedId,
            ModifiedBy = storage.ModifiedBy
        };
    }
}
