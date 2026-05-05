#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileCommandDtos
// Guid:796b28d8-3fb0-4f47-9556-dad4d408b7b4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

public sealed class FileCreateDto
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public FileType FileType { get; set; } = FileType.Other;
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public string? HashAlgorithm { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public long? ThumbnailFileId { get; set; }
    public string? UploadIp { get; set; }
    public string? UploadSource { get; set; }
    public ResourceAccessLevel AccessLevel { get; set; } = ResourceAccessLevel.Authorized;
    public string? AccessPermissions { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionKeyId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsTemporary { get; set; }
    public int RetentionDays { get; set; }
    public FileStatus Status { get; set; } = FileStatus.Normal;
    public string? Tags { get; set; }
    public string? Remark { get; set; }
    public string? ExtendData { get; set; }
}

public sealed class FileUpdateDto : BasicAppUDto
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public FileType FileType { get; set; } = FileType.Other;
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
    public string? HashAlgorithm { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public long? ThumbnailFileId { get; set; }
    public ResourceAccessLevel AccessLevel { get; set; } = ResourceAccessLevel.Authorized;
    public string? AccessPermissions { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionKeyId { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsTemporary { get; set; }
    public int RetentionDays { get; set; }
    public string? Tags { get; set; }
    public string? Remark { get; set; }
    public string? ExtendData { get; set; }
}

public sealed class FileStatusUpdateDto : BasicAppDto
{
    public FileStatus Status { get; set; } = FileStatus.Normal;
    public string? Remark { get; set; }
}

public sealed class FileStorageCreateDto
{
    public long FileId { get; set; }
    public FileStorageType StorageType { get; set; } = FileStorageType.Local;
    public string? StorageProvider { get; set; }
    public long? StorageConfigId { get; set; }
    public string? StorageRegion { get; set; }
    public string? BucketName { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? FullPath { get; set; }
    public string? InternalUrl { get; set; }
    public string? ExternalUrl { get; set; }
    public string? CdnUrl { get; set; }
    public bool IsPrimary { get; set; } = true;
    public bool IsBackup { get; set; }
    public bool EnableCdn { get; set; }
    public bool IsCompressed { get; set; }
    public decimal? CompressionRatio { get; set; }
    public FileStorageStatus Status { get; set; } = FileStorageStatus.Normal;
    public DateTimeOffset? UploadedAt { get; set; }
    public long? UploadDuration { get; set; }
    public string? UploadFailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public bool IsVerified { get; set; }
    public bool IsSynced { get; set; } = true;
    public DateTimeOffset? SyncedAt { get; set; }
    public long? SyncSourceId { get; set; }
    public string? AccessControl { get; set; }
    public string? StorageClass { get; set; }
    public string? CacheControl { get; set; }
    public int SortOrder { get; set; }
    public string? Remark { get; set; }
    public string? ExtendData { get; set; }
}

public sealed class FileStorageUpdateDto : BasicAppUDto
{
    public FileStorageType StorageType { get; set; } = FileStorageType.Local;
    public string? StorageProvider { get; set; }
    public long? StorageConfigId { get; set; }
    public string? StorageRegion { get; set; }
    public string? BucketName { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? FullPath { get; set; }
    public string? InternalUrl { get; set; }
    public string? ExternalUrl { get; set; }
    public string? CdnUrl { get; set; }
    public bool IsPrimary { get; set; } = true;
    public bool IsBackup { get; set; }
    public bool EnableCdn { get; set; }
    public bool IsCompressed { get; set; }
    public decimal? CompressionRatio { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
    public long? UploadDuration { get; set; }
    public string? UploadFailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public bool IsVerified { get; set; }
    public bool IsSynced { get; set; } = true;
    public DateTimeOffset? SyncedAt { get; set; }
    public long? SyncSourceId { get; set; }
    public string? AccessControl { get; set; }
    public string? StorageClass { get; set; }
    public string? CacheControl { get; set; }
    public int SortOrder { get; set; }
    public string? Remark { get; set; }
    public string? ExtendData { get; set; }
}

public sealed class FileStorageStatusUpdateDto : BasicAppDto
{
    public FileStorageStatus Status { get; set; } = FileStorageStatus.Normal;
    public string? UploadFailureReason { get; set; }
    public string? Remark { get; set; }
}
