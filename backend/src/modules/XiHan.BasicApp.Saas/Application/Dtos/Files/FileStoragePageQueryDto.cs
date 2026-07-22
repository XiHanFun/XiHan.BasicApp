// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统文件存储分页查询 DTO
/// </summary>
public sealed class FileStoragePageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 文件主键
    /// </summary>
    public long? FileId { get; set; }

    /// <summary>
    /// 存储类型
    /// </summary>
    public FileStorageType? StorageType { get; set; }

    /// <summary>
    /// 存储状态
    /// </summary>
    public FileStorageStatus? Status { get; set; }

    /// <summary>
    /// 是否主存储
    /// </summary>
    public bool? IsPrimary { get; set; }

    /// <summary>
    /// 是否备份存储
    /// </summary>
    public bool? IsBackup { get; set; }

    /// <summary>
    /// 是否启用 CDN
    /// </summary>
    public bool? EnableCdn { get; set; }

    /// <summary>
    /// 是否已验证
    /// </summary>
    public bool? IsVerified { get; set; }

    /// <summary>
    /// 是否已同步
    /// </summary>
    public bool? IsSynced { get; set; }

    /// <summary>
    /// 上传时间起始
    /// </summary>
    public DateTimeOffset? UploadedTimeStart { get; set; }

    /// <summary>
    /// 上传时间结束
    /// </summary>
    public DateTimeOffset? UploadedTimeEnd { get; set; }
}
