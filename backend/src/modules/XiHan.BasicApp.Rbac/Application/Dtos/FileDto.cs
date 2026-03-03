#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileDto
// Guid:3e5eb589-1488-4a35-a32f-ccf294ec181f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:27:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 文件 DTO
/// </summary>
public class FileDto : BasicAppDto
{
    public string FileName { get; set; } = string.Empty;

    public string OriginalName { get; set; } = string.Empty;

    public string? FileExtension { get; set; }

    public FileType FileType { get; set; } = FileType.Other;

    public string? MimeType { get; set; }

    public long FileSize { get; set; }

    public string? FileHash { get; set; }

    public bool IsPublic { get; set; } = true;

    public bool RequireAuth { get; set; }

    public bool IsTemporary { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public FileStatus Status { get; set; } = FileStatus.Normal;

    public string? Tags { get; set; }
}

/// <summary>
/// 创建文件 DTO
/// </summary>
public class FileCreateDto : BasicAppCDto
{
    [Required(ErrorMessage = "文件名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "文件名长度必须在 1～200 之间")]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "原始文件名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "原始文件名长度必须在 1～200 之间")]
    public string OriginalName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "扩展名长度不能超过 20")]
    public string? FileExtension { get; set; }

    public FileType FileType { get; set; } = FileType.Other;

    [StringLength(100, ErrorMessage = "MIME 类型长度不能超过 100")]
    public string? MimeType { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "文件大小不能小于 0")]
    public long FileSize { get; set; }

    [StringLength(100, ErrorMessage = "文件哈希长度不能超过 100")]
    public string? FileHash { get; set; }

    public bool IsPublic { get; set; } = true;

    public bool RequireAuth { get; set; }

    [StringLength(500, ErrorMessage = "访问权限长度不能超过 500")]
    public string? AccessPermissions { get; set; }

    public bool IsTemporary { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    [StringLength(500, ErrorMessage = "标签长度不能超过 500")]
    public string? Tags { get; set; }

    public long? TenantId { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新文件 DTO
/// </summary>
public class FileUpdateDto : BasicAppUDto
{
    [Required(ErrorMessage = "文件名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "文件名长度必须在 1～200 之间")]
    public string FileName { get; set; } = string.Empty;

    [Required(ErrorMessage = "原始文件名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "原始文件名长度必须在 1～200 之间")]
    public string OriginalName { get; set; } = string.Empty;

    [StringLength(20, ErrorMessage = "扩展名长度不能超过 20")]
    public string? FileExtension { get; set; }

    public FileType FileType { get; set; } = FileType.Other;

    [StringLength(100, ErrorMessage = "MIME 类型长度不能超过 100")]
    public string? MimeType { get; set; }

    [Range(0, long.MaxValue, ErrorMessage = "文件大小不能小于 0")]
    public long FileSize { get; set; }

    [StringLength(100, ErrorMessage = "文件哈希长度不能超过 100")]
    public string? FileHash { get; set; }

    public bool IsPublic { get; set; } = true;

    public bool RequireAuth { get; set; }

    [StringLength(500, ErrorMessage = "访问权限长度不能超过 500")]
    public string? AccessPermissions { get; set; }

    public bool IsTemporary { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public FileStatus Status { get; set; } = FileStatus.Normal;

    [StringLength(500, ErrorMessage = "标签长度不能超过 500")]
    public string? Tags { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
