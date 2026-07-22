// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.ObjectStorage.Constants;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 文件存储领域服务实现
/// </summary>
public sealed class FileStorageDomainService : IFileStorageDomainService
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico", ".tif", ".tiff"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm", ".m4v"
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".aac", ".flac", ".ogg", ".m4a", ".wma"
    };

    private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz"
    };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".csv", ".json", ".xml"
    };

    /// <inheritdoc />
    public string NormalizeOriginalName(string originalName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalName);

        var fileName = Path.GetFileName(originalName.Trim());
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("文件名不能为空。");
        }

        return fileName.Length <= 200 ? fileName : fileName[^200..];
    }

    /// <inheritdoc />
    public string BuildStoredFileName(string originalName, string fileHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileHash);

        var extension = Path.GetExtension(NormalizeOriginalName(originalName)).ToLowerInvariant();
        var hashPrefix = fileHash.Trim();
        if (hashPrefix.Length > 32)
        {
            hashPrefix = hashPrefix[..32];
        }

        return $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{hashPrefix}{extension}";
    }

    /// <inheritdoc />
    public string BuildStoragePath(string fileName, string? directory, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var normalizedFileName = Path.GetFileName(fileName.Trim());
        var normalizedDirectory = NormalizePathSegment(directory);
        var dateSegment = $"{now:yyyy/MM/dd}";

        return string.IsNullOrWhiteSpace(normalizedDirectory)
            ? $"uploads/{dateSegment}/{normalizedFileName}"
            : $"{normalizedDirectory}/{dateSegment}/{normalizedFileName}";
    }

    /// <inheritdoc />
    public FileType ResolveFileType(string? extension, string? mimeType)
    {
        var normalizedExtension = NormalizeExtension(extension);
        var normalizedMimeType = mimeType?.Trim().ToLowerInvariant();

        if (normalizedMimeType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true ||
            ImageExtensions.Contains(normalizedExtension))
        {
            return FileType.Image;
        }

        if (normalizedMimeType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true ||
            VideoExtensions.Contains(normalizedExtension))
        {
            return FileType.Video;
        }

        if (normalizedMimeType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) == true ||
            AudioExtensions.Contains(normalizedExtension))
        {
            return FileType.Audio;
        }

        if (ArchiveExtensions.Contains(normalizedExtension))
        {
            return FileType.Archive;
        }

        if (DocumentExtensions.Contains(normalizedExtension) ||
            normalizedMimeType is not null && (normalizedMimeType.Contains("text/", StringComparison.OrdinalIgnoreCase) ||
                                               normalizedMimeType.Contains("document", StringComparison.OrdinalIgnoreCase) ||
                                               normalizedMimeType.Contains("pdf", StringComparison.OrdinalIgnoreCase)))
        {
            return FileType.Document;
        }

        return FileType.Other;
    }

    /// <inheritdoc />
    public FileStorageType ResolveStorageType(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        return providerName.Trim() switch
        {
            ObjectStorageProviderNames.Local => FileStorageType.Local,
            ObjectStorageProviderNames.Minio => FileStorageType.Minio,
            ObjectStorageProviderNames.AliyunOss => FileStorageType.AliyunOss,
            ObjectStorageProviderNames.TencentCos => FileStorageType.TencentCos,
            _ => FileStorageType.Custom
        };
    }

    /// <inheritdoc />
    public string ResolveAccessControl(ResourceAccessLevel accessLevel, string? accessControl)
    {
        if (!string.IsNullOrWhiteSpace(accessControl))
        {
            return accessControl.Trim();
        }

        return accessLevel == ResourceAccessLevel.Public ? "public-read" : "private";
    }

    /// <inheritdoc />
    public void EnsureUploadMetadata(long fileSize, bool isTemporary, DateTimeOffset? expiresAt, int retentionDays)
    {
        if (fileSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSize), "文件大小必须大于 0。");
        }

        if (retentionDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retentionDays), "保留天数不能小于 0。");
        }

        if (isTemporary && !expiresAt.HasValue && retentionDays <= 0)
        {
            throw new InvalidOperationException("临时文件必须设置过期时间或保留天数。");
        }

        if (expiresAt.HasValue && expiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException("文件过期时间必须晚于当前时间。");
        }
    }

    /// <inheritdoc />
    public void EnsureCanBecomePrimary(SysFileStorage storage)
    {
        ArgumentNullException.ThrowIfNull(storage);

        if (storage.Status != FileStorageStatus.Normal)
        {
            throw new InvalidOperationException("只有正常状态的存储副本可以设为主存储。");
        }

        if (!storage.IsVerified)
        {
            throw new InvalidOperationException("未通过校验的存储副本不能设为主存储。");
        }
    }

    private static string? NormalizePathSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value
            .Trim()
            .Replace('\\', '/')
            .Trim('/');

        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("存储目录不能包含上级路径。");
        }

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalized = extension.Trim().ToLowerInvariant();
        return normalized.StartsWith('.') ? normalized : $".{normalized}";
    }
}
