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
    public FileType FileType { get; set; } = FileType.Other;

    /// <summary>
    /// MIME 类型
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件哈希
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 是否需要认证
    /// </summary>
    public bool RequireAuth { get; set; }

    /// <summary>
    /// 是否临时文件
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 文件状态
    /// </summary>
    public FileStatus Status { get; set; } = FileStatus.Normal;

    /// <summary>
    /// 标签
    /// </summary>
    public string? Tags { get; set; }
}

/// <summary>
/// 创建文件 DTO
/// </summary>
public class FileCreateDto : BasicAppCDto
{
    /// <summary>
    /// 文件名
    /// </summary>
    [Required(ErrorMessage = "文件名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "文件名长度必须在 1～200 之间")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [Required(ErrorMessage = "原始文件名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "原始文件名长度必须在 1～200 之间")]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [StringLength(20, ErrorMessage = "扩展名长度不能超过 20")]
    public string? FileExtension { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    public FileType FileType { get; set; } = FileType.Other;

    /// <summary>
    /// MIME 类型
    /// </summary>
    [StringLength(100, ErrorMessage = "MIME 类型长度不能超过 100")]
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "文件大小不能小于 0")]
    public long FileSize { get; set; }

    /// <summary>
    /// 文件哈希
    /// </summary>
    [StringLength(100, ErrorMessage = "文件哈希长度不能超过 100")]
    public string? FileHash { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 是否需要认证
    /// </summary>
    public bool RequireAuth { get; set; }

    /// <summary>
    /// 访问权限
    /// </summary>
    [StringLength(500, ErrorMessage = "访问权限长度不能超过 500")]
    public string? AccessPermissions { get; set; }

    /// <summary>
    /// 是否临时文件
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    [StringLength(500, ErrorMessage = "标签长度不能超过 500")]
    public string? Tags { get; set; }

    /// <summary>
    /// 租户标识
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新文件 DTO
/// </summary>
public class FileUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 文件名
    /// </summary>
    [Required(ErrorMessage = "文件名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "文件名长度必须在 1～200 之间")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 原始文件名
    /// </summary>
    [Required(ErrorMessage = "原始文件名不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "原始文件名长度必须在 1～200 之间")]
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名
    /// </summary>
    [StringLength(20, ErrorMessage = "扩展名长度不能超过 20")]
    public string? FileExtension { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    public FileType FileType { get; set; } = FileType.Other;

    /// <summary>
    /// MIME 类型
    /// </summary>
    [StringLength(100, ErrorMessage = "MIME 类型长度不能超过 100")]
    public string? MimeType { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    [Range(0, long.MaxValue, ErrorMessage = "文件大小不能小于 0")]
    public long FileSize { get; set; }

    /// <summary>
    /// 文件哈希
    /// </summary>
    [StringLength(100, ErrorMessage = "文件哈希长度不能超过 100")]
    public string? FileHash { get; set; }

    /// <summary>
    /// 是否公开访问
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 是否需要认证
    /// </summary>
    public bool RequireAuth { get; set; }

    /// <summary>
    /// 访问权限
    /// </summary>
    [StringLength(500, ErrorMessage = "访问权限长度不能超过 500")]
    public string? AccessPermissions { get; set; }

    /// <summary>
    /// 是否临时文件
    /// </summary>
    public bool IsTemporary { get; set; }

    /// <summary>
    /// 过期时间
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 文件状态
    /// </summary>
    public FileStatus Status { get; set; } = FileStatus.Normal;

    /// <summary>
    /// 标签
    /// </summary>
    [StringLength(500, ErrorMessage = "标签长度不能超过 500")]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
