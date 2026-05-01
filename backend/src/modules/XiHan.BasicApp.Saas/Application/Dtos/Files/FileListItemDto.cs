#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileListItemDto
// Guid:a31c7786-40c9-46cc-828e-61f1cfb6628f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统文件列表项 DTO
/// </summary>
public class FileListItemDto : BasicAppDto
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    public FileType FileType { get; set; }

    /// <summary>
    /// MIME 类型
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 宽度
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// 高度
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// 时长
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// 缩略图文件主键
    /// </summary>
    public long? ThumbnailFileId { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// 访问次数
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// 最后下载时间
    /// </summary>
    public DateTimeOffset? LastDownloadTime { get; set; }

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTimeOffset? LastAccessTime { get; set; }

    /// <summary>
    /// 访问级别
    /// </summary>
    public ResourceAccessLevel AccessLevel { get; set; }

    /// <summary>
    /// 是否加密存储
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 是否临时文件
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 保留天数
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// 文件状态
    /// </summary>
    public FileStatus Status { get; set; }

    /// <summary>
    /// 是否包含完整性指纹
    /// </summary>
    public bool HasIntegrityFingerprint { get; set; }

    /// <summary>
    /// 是否包含访问规则
    /// </summary>
    public bool HasAccessRule { get; set; }

    /// <summary>
    /// 是否包含标签
    /// </summary>
    public bool HasLabels { get; set; }

    /// <summary>
    /// 是否包含备注
    /// </summary>
    public bool HasNote { get; set; }

    /// <summary>
    /// 是否包含扩展信息
    /// </summary>
    public bool HasExtension { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
