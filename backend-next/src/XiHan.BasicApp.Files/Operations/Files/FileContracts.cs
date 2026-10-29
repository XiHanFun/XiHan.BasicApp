// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;

namespace XiHan.BasicApp.Files.Operations.Files;

/// <summary>
/// 一个文件。
/// </summary>
/// <param name="Id">文件 ID。</param>
/// <param name="Name">系统文件名。</param>
/// <param name="OriginalName">原始文件名。</param>
/// <param name="Extension">扩展名（小写含点）。</param>
/// <param name="Kind">文件大类。</param>
/// <param name="ContentType">内容类型。</param>
/// <param name="Size">大小（字节）。</param>
/// <param name="Hash">SHA-256。</param>
/// <param name="Status">状态。</param>
/// <param name="AccessLevel">访问级别。</param>
/// <param name="IsTemporary">是否临时文件。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="Source">来源。</param>
/// <param name="Tags">标签（逗号分隔）。</param>
/// <param name="Remark">备注。</param>
/// <param name="DownloadCount">下载次数。</param>
/// <param name="LastDownloadedAt">最近一次下载时间。</param>
/// <param name="CreatedAt">上传时间。</param>
/// <param name="CreatedBy">上传人（用户 ID）。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record FileResponse(
    Guid Id,
    string Name,
    string OriginalName,
    string? Extension,
    FileKind Kind,
    string ContentType,
    long Size,
    string Hash,
    FileStatus Status,
    FileAccessLevel AccessLevel,
    bool IsTemporary,
    DateTimeOffset? ExpiresAt,
    string Source,
    string? Tags,
    string? Remark,
    int DownloadCount,
    DateTimeOffset? LastDownloadedAt,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 由文件投影。
    /// </summary>
    /// <param name="file">文件。</param>
    /// <returns>响应。</returns>
    public static FileResponse From(SysFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        return new(file.Id, file.Name, file.OriginalName, file.Extension, file.Kind, file.ContentType, file.Size, file.Hash, file.Status, file.AccessLevel, file.IsTemporary, file.ExpiresAt, file.Source, file.Tags, file.Remark, file.DownloadCount, file.LastDownloadedAt, file.CreatedAt, file.CreatedBy, file.ModifiedAt);
    }
}

/// <summary>
/// <c>files.get</c> 的请求。
/// </summary>
/// <param name="Id">文件 ID。</param>
public sealed record GetFileRequest([Required] Guid Id);

/// <summary>
/// <c>files.update</c> 的请求：修改访问级别、临时与到期、标签与备注。
/// </summary>
/// <param name="Id">文件 ID。</param>
/// <param name="AccessLevel">访问级别。</param>
/// <param name="IsTemporary">是否临时文件。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="Tags">标签（逗号分隔）。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateFileRequest(
    [Required] Guid Id,
    FileAccessLevel AccessLevel = FileAccessLevel.Authorized,
    bool IsTemporary = false,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(500)] string? Tags = null,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成附加信息。
    /// </summary>
    /// <returns>附加信息。</returns>
    public FileDetails ToDetails() => new(AccessLevel, IsTemporary, ExpiresAt, Tags, Remark);
}

/// <summary>
/// <c>files.set-status</c> 的请求：归档或恢复文件。
/// </summary>
/// <remarks>
/// 归档为 <c>Archived</c>，恢复为 <c>Normal</c>。
/// </remarks>
/// <param name="Ids">文件 ID。</param>
/// <param name="Status">目标状态。</param>
public sealed record SetFileStatusRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids, FileStatus Status);

/// <summary>
/// <c>files.delete</c> 的请求。
/// </summary>
/// <param name="Ids">文件 ID。</param>
public sealed record FileIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

/// <summary>
/// <c>files.presign</c> 的请求。
/// </summary>
/// <param name="Id">文件 ID。</param>
/// <param name="ExpiresInSeconds">有效期（秒）；不填取缺省值（30 分钟）。</param>
public sealed record PresignFileRequest([Required] Guid Id, [Range(60, 604800)] int? ExpiresInSeconds = null);

/// <summary>
/// 签名下载地址。
/// </summary>
/// <param name="Url">地址（相对接口根，如 <c>/api/files/content/&lt;令牌&gt;</c>）；加查询 <c>download=true</c> 即以附件下载。</param>
/// <param name="ExpiresAt">到期时间。</param>
public sealed record PresignFileResponse(string Url, DateTimeOffset ExpiresAt);

/// <summary>
/// <c>files.fast-upload</c> 的请求。
/// </summary>
/// <param name="Hash">内容的 SHA-256（64 位十六进制）。</param>
/// <param name="Size">大小（字节）。</param>
/// <param name="OriginalName">原始文件名。</param>
/// <param name="ContentType">内容类型。</param>
/// <param name="AccessLevel">访问级别。</param>
/// <param name="IsTemporary">是否临时文件。</param>
/// <param name="ExpiresAt">到期时间。</param>
/// <param name="Tags">标签（逗号分隔）。</param>
/// <param name="Remark">备注。</param>
public sealed record FastUploadFileRequest(
    [Required, StringLength(64, MinimumLength = 64)] string Hash,
    [Range(1, long.MaxValue)] long Size,
    [Required, StringLength(255, MinimumLength = 1)] string OriginalName,
    [StringLength(100)] string? ContentType = null,
    FileAccessLevel AccessLevel = FileAccessLevel.Authorized,
    bool IsTemporary = false,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(500)] string? Tags = null,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成附加信息。
    /// </summary>
    /// <returns>附加信息。</returns>
    public FileDetails ToDetails() => new(AccessLevel, IsTemporary, ExpiresAt, Tags, Remark);
}

/// <summary>
/// 秒传的结果。
/// </summary>
/// <param name="File">新文件；没命中为空，前端再走普通上传。</param>
public sealed record FastUploadFileResponse(FileResponse? File);

/// <summary>
/// 一个文件副本。
/// </summary>
/// <param name="Id">副本 ID。</param>
/// <param name="StorageConfigId">存储配置 ID。</param>
/// <param name="StorageConfigCode">存储配置编码；配置已删为空。</param>
/// <param name="StorageConfigName">存储配置名称。</param>
/// <param name="StorageType">写入时的存储类型。</param>
/// <param name="Bucket">桶名。</param>
/// <param name="Key">对象键。</param>
/// <param name="IsPrimary">是否主副本。</param>
/// <param name="Status">状态。</param>
/// <param name="VerifiedAt">最近一次校验时间。</param>
/// <param name="Remark">备注（校验失败的原因：<c>missing</c> 内容缺失、<c>hash-mismatch</c> 内容不符）。</param>
/// <param name="CreatedAt">写入时间。</param>
public sealed record FileStorageResponse(
    Guid Id,
    Guid StorageConfigId,
    string? StorageConfigCode,
    string? StorageConfigName,
    StorageType StorageType,
    string Bucket,
    string Key,
    bool IsPrimary,
    FileStorageStatus Status,
    DateTimeOffset? VerifiedAt,
    string? Remark,
    DateTimeOffset CreatedAt);

/// <summary>
/// <c>files.storages</c> 的响应。
/// </summary>
/// <param name="Items">副本（主副本在前）。</param>
public sealed record FileStoragesResponse(IReadOnlyList<FileStorageResponse> Items);

/// <summary>
/// 针对一个副本的请求。
/// </summary>
/// <remarks>
/// 用于 <c>files.set-primary-storage</c>、<c>files.verify-storage</c>。
/// </remarks>
/// <param name="Id">文件 ID。</param>
/// <param name="StorageId">副本 ID。</param>
public sealed record FileStorageRequest([Required] Guid Id, [Required] Guid StorageId);

/// <summary>
/// <c>files.set-storage-status</c> 的请求。
/// </summary>
/// <param name="Id">文件 ID。</param>
/// <param name="StorageId">副本 ID。</param>
/// <param name="Status">目标状态：<c>Normal</c> 或 <c>Unavailable</c>。</param>
public sealed record SetFileStorageStatusRequest([Required] Guid Id, [Required] Guid StorageId, FileStorageStatus Status);
