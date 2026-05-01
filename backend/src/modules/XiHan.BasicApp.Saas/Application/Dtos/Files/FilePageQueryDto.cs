#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FilePageQueryDto
// Guid:455cbac2-ec91-42fb-819b-8ea18a70540d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统文件分页查询 DTO
/// </summary>
public sealed class FilePageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 文件类型
    /// </summary>
    public FileType? FileType { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// MIME 类型
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// 访问级别
    /// </summary>
    public ResourceAccessLevel? AccessLevel { get; set; }

    /// <summary>
    /// 是否加密存储
    /// </summary>
    public bool? IsEncrypted { get; set; }

    /// <summary>
    /// 是否临时文件
    /// </summary>
    public bool? IsTemporary { get; set; }

    /// <summary>
    /// 文件状态
    /// </summary>
    public FileStatus? Status { get; set; }

    /// <summary>
    /// 过期时间起始
    /// </summary>
    public DateTimeOffset? ExpiresAtStart { get; set; }

    /// <summary>
    /// 过期时间结束
    /// </summary>
    public DateTimeOffset? ExpiresAtEnd { get; set; }
}
