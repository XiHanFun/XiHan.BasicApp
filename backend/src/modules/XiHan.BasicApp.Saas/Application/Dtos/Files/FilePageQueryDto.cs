// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    public DateTimeOffset? ExpirationTimeStart { get; set; }

    /// <summary>
    /// 过期时间结束
    /// </summary>
    public DateTimeOffset? ExpirationTimeEnd { get; set; }
}
