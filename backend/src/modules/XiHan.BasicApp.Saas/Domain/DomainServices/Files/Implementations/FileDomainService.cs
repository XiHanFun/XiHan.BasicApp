#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileDomainService
// Guid:ff2f94fa-59bf-4d86-b645-76d0cdab6a8f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 文件领域服务实现
/// </summary>
public sealed class FileDomainService
    : IFileDomainService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public FileDomainService(
        IFileRepository fileRepository,
        IFileStorageRepository fileStorageRepository,
        IFileStorageDomainService fileStorageDomainService)
    {
        _fileRepository = fileRepository;
        _fileStorageRepository = fileStorageRepository;
        _fileStorageDomainService = fileStorageDomainService;
    }

    private readonly IFileRepository _fileRepository;
    private readonly IFileStorageDomainService _fileStorageDomainService;
    private readonly IFileStorageRepository _fileStorageRepository;

    /// <inheritdoc />
    public async Task<FileDeleteCommandResult> DeleteFileAsync(FileDeleteCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var file = await GetFileOrThrowAsync(command.BasicId, cancellationToken);
        var storages = await _fileStorageRepository.GetByFileIdAsync(file.BasicId, cancellationToken);
        file.Status = FileStatus.Deleted;
        file.Remark = Optional(command.Reason, 500, nameof(command.Reason), "删除原因不能超过 500 个字符。") ?? file.Remark;
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

        return new FileDeleteCommandResult(file);
    }

    /// <inheritdoc />
    public async Task<FileCommandResult> FastUploadFileAsync(FileFastUploadCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var fileHash = Required(command.FileHash, 100, nameof(command.FileHash), "文件哈希不能超过 100 个字符。");
        _fileStorageDomainService.EnsureUploadMetadata(command.FileSize, command.IsTemporary, command.ExpiresAt, command.RetentionDays);

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

        ApplyFastUploadMetadata(existing, command);
        return new FileCommandResult(await _fileRepository.UpdateAsync(existing, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<FilePrimaryStorageSwitchCommandResult> SwitchPrimaryStorageAsync(FilePrimaryStorageSwitchCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统文件主键必须大于 0。");
        EnsureId(command.StorageId, "系统文件存储主键必须大于 0。");
        _ = await GetFileOrThrowAsync(command.BasicId, cancellationToken);
        var storage = await GetFileStorageOrThrowAsync(command.StorageId, cancellationToken);
        if (storage.FileId != command.BasicId)
        {
            throw new InvalidOperationException("存储副本不属于当前文件。");
        }

        _fileStorageDomainService.EnsureCanBecomePrimary(storage);
        var previousStorage = await _fileStorageRepository.GetPrimaryByFileIdAsync(command.BasicId, cancellationToken);
        await _fileStorageRepository.ClearPrimaryAsync(command.BasicId, storage.BasicId, cancellationToken);
        storage.IsPrimary = true;
        storage.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? storage.Remark;
        storage = await _fileStorageRepository.UpdateAsync(storage, cancellationToken);

        return new FilePrimaryStorageSwitchCommandResult(storage, previousStorage?.BasicId);
    }

    /// <inheritdoc />
    public async Task<FileCommandResult> UpdateFileMetadataAsync(FileMetadataUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统文件主键必须大于 0。");
        ValidateMutableMetadata(command.AccessLevel, command.RetentionDays, command.Width, command.Height, command.Duration, command.ThumbnailFileId);
        _fileStorageDomainService.EnsureUploadMetadata(1, command.IsTemporary, command.ExpiresAt, command.RetentionDays);

        var file = await GetFileOrThrowAsync(command.BasicId, cancellationToken);
        ApplyMutableMetadata(file, command);
        return new FileCommandResult(await _fileRepository.UpdateAsync(file, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<FileCommandResult> UpdateFileStatusAsync(FileStatusUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统文件主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));
        var file = await GetFileOrThrowAsync(command.BasicId, cancellationToken);
        file.Status = command.Status;
        file.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? file.Remark;

        return new FileCommandResult(await _fileRepository.UpdateAsync(file, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<FileStorageCommandResult> UpdateFileStorageStatusAsync(FileStorageStatusUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "系统文件存储主键必须大于 0。");
        EnsureEnum(command.Status, nameof(command.Status));
        var storage = await GetFileStorageOrThrowAsync(command.BasicId, cancellationToken);
        storage.Status = command.Status;
        storage.UploadFailureReason = Optional(command.UploadFailureReason, 500, nameof(command.UploadFailureReason), "上传失败原因不能超过 500 个字符。");
        storage.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? storage.Remark;

        return new FileStorageCommandResult(await _fileStorageRepository.UpdateAsync(storage, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<FileCommandResult> CreateUploadingFileAsync(FileCreateUploadingCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateMutableMetadata(command.AccessLevel, command.RetentionDays, command.Width, command.Height, command.Duration, command.ThumbnailFileId);
        _fileStorageDomainService.EnsureUploadMetadata(command.FileSize, command.IsTemporary, command.ExpiresAt, command.RetentionDays);

        var file = new SysFile
        {
            FileName = Required(command.FileName, 200, nameof(command.FileName), "文件名不能超过 200 个字符。"),
            OriginalName = Required(command.OriginalName, 200, nameof(command.OriginalName), "原始文件名不能超过 200 个字符。"),
            FileExtension = Optional(command.FileExtension, 20, nameof(command.FileExtension), "文件扩展名不能超过 20 个字符。"),
            FileType = command.FileType,
            MimeType = Optional(command.MimeType, 100, nameof(command.MimeType), "MIME 类型不能超过 100 个字符。"),
            FileSize = command.FileSize,
            FileHash = Required(command.FileHash, 100, nameof(command.FileHash), "文件哈希不能超过 100 个字符。"),
            HashAlgorithm = Required(command.HashAlgorithm, 20, nameof(command.HashAlgorithm), "哈希算法不能超过 20 个字符。"),
            Width = command.Width,
            Height = command.Height,
            Duration = command.Duration,
            ThumbnailFileId = command.ThumbnailFileId,
            UploadIp = Optional(command.UploadIp, 50, nameof(command.UploadIp), "上传 IP 不能超过 50 个字符。"),
            UploadSource = Optional(command.UploadSource, 50, nameof(command.UploadSource), "上传来源不能超过 50 个字符。"),
            AccessLevel = command.AccessLevel,
            AccessPermissions = Optional(command.AccessPermissions, 500, nameof(command.AccessPermissions), "访问权限不能超过 500 个字符。"),
            IsEncrypted = command.IsEncrypted,
            EncryptionKeyId = Optional(command.EncryptionKeyId, 100, nameof(command.EncryptionKeyId), "加密密钥主键不能超过 100 个字符。"),
            ExpiresAt = command.ExpiresAt,
            IsTemporary = command.IsTemporary,
            RetentionDays = command.RetentionDays,
            Status = FileStatus.Uploading,
            Tags = Optional(command.Tags, 500, nameof(command.Tags), "文件标签不能超过 500 个字符。"),
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。"),
            ExtendData = OptionalJson(command.ExtendData, "文件扩展数据必须是合法 JSON。")
        };

        return new FileCommandResult(await _fileRepository.AddAsync(file, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<FileCommandResult> MarkUploadFailedAsync(FileUploadFailedCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var file = await GetFileOrThrowAsync(command.FileId, cancellationToken);
        file.Status = FileStatus.UploadFailed;
        file.Remark = Optional(command.ErrorMessage, 500, nameof(command.ErrorMessage), "上传失败原因不能超过 500 个字符。") ?? file.Remark;
        return new FileCommandResult(await _fileRepository.UpdateAsync(file, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<FileUploadCommandResult> CompleteUploadAsync(FileUploadCompleteCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var file = await GetFileOrThrowAsync(command.FileId, cancellationToken);
        file.FileSize = command.UploadedFileSize > 0 ? command.UploadedFileSize : file.FileSize;
        file.Status = FileStatus.Normal;
        file = await _fileRepository.UpdateAsync(file, cancellationToken);

        var storage = new SysFileStorage
        {
            FileId = file.BasicId,
            StorageType = command.StorageType,
            StorageProvider = Optional(command.StorageProvider, 50, nameof(command.StorageProvider), "存储提供商不能超过 50 个字符。"),
            BucketName = Optional(command.BucketName, 100, nameof(command.BucketName), "存储桶名称不能超过 100 个字符。"),
            StoragePath = Required(command.StoragePath, 500, nameof(command.StoragePath), "存储路径不能超过 500 个字符。"),
            FullPath = Optional(command.FullPath, 1000, nameof(command.FullPath), "完整路径不能超过 1000 个字符。"),
            InternalUrl = Optional(command.InternalUrl, 1000, nameof(command.InternalUrl), "内部访问 URL 不能超过 1000 个字符。"),
            ExternalUrl = Optional(command.ExternalUrl, 1000, nameof(command.ExternalUrl), "外部访问 URL 不能超过 1000 个字符。"),
            IsPrimary = true,
            IsBackup = false,
            Status = FileStorageStatus.Normal,
            UploadedAt = command.UploadedAt,
            UploadDuration = command.UploadDuration,
            LastVerifiedAt = command.UploadedAt,
            IsVerified = true,
            IsSynced = true,
            SyncedAt = command.UploadedAt,
            AccessControl = Required(command.AccessControl, 50, nameof(command.AccessControl), "访问控制不能超过 50 个字符。"),
            CacheControl = Optional(command.CacheControl, 100, nameof(command.CacheControl), "缓存控制不能超过 100 个字符。"),
            SortOrder = 0,
            Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。")
        };

        await _fileStorageRepository.ClearPrimaryAsync(file.BasicId, excludeStorageId: null, cancellationToken);
        storage = await _fileStorageRepository.AddAsync(storage, cancellationToken);
        return new FileUploadCommandResult(file, storage);
    }

    /// <inheritdoc />
    public async Task<FileStorageCommandResult> VerifyFileStorageAsync(FileStorageVerifyCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var storage = await GetFileStorageOrThrowAsync(command.StorageId, cancellationToken);
        var file = await GetFileOrThrowAsync(storage.FileId, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        storage.LastVerifiedAt = now;
        storage.IsVerified = command.Exists;
        storage.Status = command.Exists ? FileStorageStatus.Normal : FileStorageStatus.VerificationFailed;
        storage.UploadFailureReason = command.Exists ? null : "对象存储文件不存在。";
        storage.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? storage.Remark;

        if (command.Exists)
        {
            storage.ExternalUrl = Optional(command.ExternalUrl, 1000, nameof(command.ExternalUrl), "外部访问 URL 不能超过 1000 个字符。") ?? storage.ExternalUrl;
            file.FileSize = command.MetadataSize > 0 ? command.MetadataSize : file.FileSize;
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
        return new FileStorageCommandResult(storage);
    }

    private static void ApplyFastUploadMetadata(SysFile file, FileFastUploadCommand command)
    {
        file.OriginalName = Required(command.OriginalName, 200, nameof(command.OriginalName), "原始文件名不能超过 200 个字符。");
        file.FileExtension = Optional(command.FileExtension, 20, nameof(command.FileExtension), "文件扩展名不能超过 20 个字符。") ?? file.FileExtension;
        file.MimeType = Optional(command.MimeType, 100, nameof(command.MimeType), "MIME 类型不能超过 100 个字符。") ?? file.MimeType;
        file.AccessLevel = command.AccessLevel;
        file.AccessPermissions = Optional(command.AccessPermissions, 500, nameof(command.AccessPermissions), "访问权限不能超过 500 个字符。");
        file.ExpiresAt = command.ExpiresAt;
        file.IsTemporary = command.IsTemporary;
        file.RetentionDays = command.RetentionDays;
        file.Tags = Optional(command.Tags, 500, nameof(command.Tags), "文件标签不能超过 500 个字符。") ?? file.Tags;
        file.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。") ?? file.Remark;
        file.ExtendData = OptionalJson(command.ExtendData, "文件扩展数据必须是合法 JSON。") ?? file.ExtendData;
    }

    private static void ApplyMutableMetadata(SysFile file, FileMetadataUpdateCommand command)
    {
        file.Width = command.Width;
        file.Height = command.Height;
        file.Duration = command.Duration;
        file.ThumbnailFileId = command.ThumbnailFileId;
        file.AccessLevel = command.AccessLevel;
        file.AccessPermissions = Optional(command.AccessPermissions, 500, nameof(command.AccessPermissions), "访问权限不能超过 500 个字符。");
        file.IsEncrypted = command.IsEncrypted;
        file.EncryptionKeyId = Optional(command.EncryptionKeyId, 100, nameof(command.EncryptionKeyId), "加密密钥主键不能超过 100 个字符。");
        file.ExpiresAt = command.ExpiresAt;
        file.IsTemporary = command.IsTemporary;
        file.RetentionDays = command.RetentionDays;
        file.Tags = Optional(command.Tags, 500, nameof(command.Tags), "文件标签不能超过 500 个字符。");
        file.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
        file.ExtendData = OptionalJson(command.ExtendData, "文件扩展数据必须是合法 JSON。");
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

    private static void EnsureEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static void EnsureNonNegative(long value, string paramName, string message)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void EnsureOptionalId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void EnsureOptionalNonNegative(long? value, string paramName, string message)
    {
        if (value is < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }

    private static string? OptionalJson(string? value, string message)
    {
        var normalized = NormalizeNullable(value);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(message, exception);
        }

        return normalized;
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }

        return normalized;
    }
}
