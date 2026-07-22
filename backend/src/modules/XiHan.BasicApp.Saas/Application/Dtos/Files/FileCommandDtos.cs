// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

public sealed class FileUploadDto
{
    public IFormFile File { get; set; } = null!;
    public string? RouteKey { get; set; }
    public string? ProviderName { get; set; }
    public string? Directory { get; set; }
    public string? BucketName { get; set; }
    public bool Overwrite { get; set; }
    public string? AccessControl { get; set; }
    public string? CacheControl { get; set; }
    public ResourceAccessLevel AccessLevel { get; set; } = ResourceAccessLevel.Authorized;
    public string? AccessPermissions { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionKeyId { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
    public bool IsTemporary { get; set; }
    public int RetentionDays { get; set; }
    public string? Tags { get; set; }
    public string? Remark { get; set; }
    public string? ExtendData { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public long? ThumbnailFileId { get; set; }
}

public sealed class FileFastUploadDto
{
    public string FileHash { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? FileExtension { get; set; }
    public ResourceAccessLevel AccessLevel { get; set; } = ResourceAccessLevel.Authorized;
    public string? AccessPermissions { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
    public bool IsTemporary { get; set; }
    public int RetentionDays { get; set; }
    public string? Tags { get; set; }
    public string? Remark { get; set; }
    public string? ExtendData { get; set; }
}

public sealed class FileMetadataUpdateDto : BasicAppUDto
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public long? ThumbnailFileId { get; set; }
    public ResourceAccessLevel AccessLevel { get; set; } = ResourceAccessLevel.Authorized;
    public string? AccessPermissions { get; set; }
    public bool IsEncrypted { get; set; }
    public string? EncryptionKeyId { get; set; }
    public DateTimeOffset? ExpirationTime { get; set; }
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

public sealed class FilePrimaryStorageSwitchDto : BasicAppDto
{
    public long StorageId { get; set; }
    public string? Remark { get; set; }
}

public sealed class FileStorageVerifyDto : BasicAppDto
{
    public string? Remark { get; set; }
}

public sealed class FileStorageStatusUpdateDto : BasicAppDto
{
    public FileStorageStatus Status { get; set; } = FileStorageStatus.Normal;
    public string? UploadFailureReason { get; set; }
    public string? Remark { get; set; }
}

public sealed class FileDeleteDto : BasicAppDto
{
    public bool DeletePhysical { get; set; }
    public string? Reason { get; set; }
}
