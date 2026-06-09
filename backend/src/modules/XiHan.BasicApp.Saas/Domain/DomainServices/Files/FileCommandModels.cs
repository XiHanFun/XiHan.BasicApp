#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileCommandModels
// Guid:cb36f217-fafa-460d-8105-30be45a47162
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 文件秒传命令
/// </summary>
public sealed record FileFastUploadCommand(
    string FileHash,
    string OriginalName,
    long FileSize,
    string? MimeType,
    string? FileExtension,
    ResourceAccessLevel AccessLevel,
    string? AccessPermissions,
    DateTimeOffset? ExpirationTime,
    bool IsTemporary,
    int RetentionDays,
    string? Tags,
    string? Remark,
    string? ExtendData);

/// <summary>
/// 文件元数据更新命令
/// </summary>
public sealed record FileMetadataUpdateCommand(
    long BasicId,
    int? Width,
    int? Height,
    int? Duration,
    long? ThumbnailFileId,
    ResourceAccessLevel AccessLevel,
    string? AccessPermissions,
    bool IsEncrypted,
    string? EncryptionKeyId,
    DateTimeOffset? ExpirationTime,
    bool IsTemporary,
    int RetentionDays,
    string? Tags,
    string? Remark,
    string? ExtendData);

/// <summary>
/// 文件状态更新命令
/// </summary>
public sealed record FileStatusUpdateCommand(long BasicId, FileStatus Status, string? Remark);

/// <summary>
/// 文件存储状态更新命令
/// </summary>
public sealed record FileStorageStatusUpdateCommand(long BasicId, FileStorageStatus Status, string? UploadFailureReason, string? Remark);

/// <summary>
/// 文件主存储切换命令
/// </summary>
public sealed record FilePrimaryStorageSwitchCommand(long BasicId, long StorageId, string? Remark);

/// <summary>
/// 文件删除命令
/// </summary>
public sealed record FileDeleteCommand(long BasicId, string? Reason);

/// <summary>
/// 上传中文件创建命令
/// </summary>
public sealed record FileCreateUploadingCommand(
    string FileName,
    string OriginalName,
    string? FileExtension,
    FileType FileType,
    string? MimeType,
    long FileSize,
    string FileHash,
    string HashAlgorithm,
    int? Width,
    int? Height,
    int? Duration,
    long? ThumbnailFileId,
    string? UploadIp,
    string? UploadSource,
    ResourceAccessLevel AccessLevel,
    string? AccessPermissions,
    bool IsEncrypted,
    string? EncryptionKeyId,
    DateTimeOffset? ExpirationTime,
    bool IsTemporary,
    int RetentionDays,
    string? Tags,
    string? Remark,
    string? ExtendData);

/// <summary>
/// 文件上传完成命令
/// </summary>
public sealed record FileUploadCompleteCommand(
    long FileId,
    long UploadedFileSize,
    FileStorageType StorageType,
    string? StorageProvider,
    string? BucketName,
    string StoragePath,
    string? FullPath,
    string? InternalUrl,
    string? ExternalUrl,
    DateTimeOffset UploadedTime,
    long? UploadDuration,
    string AccessControl,
    string? CacheControl,
    string? Remark);

/// <summary>
/// 文件上传失败命令
/// </summary>
public sealed record FileUploadFailedCommand(long FileId, string? ErrorMessage);

/// <summary>
/// 文件存储校验命令
/// </summary>
public sealed record FileStorageVerifyCommand(long StorageId, bool Exists, long MetadataSize, string? ExternalUrl, string? Remark);

/// <summary>
/// 文件命令结果
/// </summary>
public sealed record FileCommandResult(SysFile File);

/// <summary>
/// 文件存储命令结果
/// </summary>
public sealed record FileStorageCommandResult(SysFileStorage Storage);

/// <summary>
/// 文件上传命令结果
/// </summary>
public sealed record FileUploadCommandResult(SysFile File, SysFileStorage Storage);

/// <summary>
/// 主存储切换命令结果
/// </summary>
public sealed record FilePrimaryStorageSwitchCommandResult(SysFileStorage Storage, long? PreviousStorageId);

/// <summary>
/// 文件删除命令结果
/// </summary>
public sealed record FileDeleteCommandResult(SysFile File);
