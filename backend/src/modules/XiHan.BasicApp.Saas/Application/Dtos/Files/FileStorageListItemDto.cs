#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FileStorageListItemDto
// Guid:85e2b126-16ea-4bb2-a3d4-f5236dc07f70
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统文件存储列表项 DTO
/// </summary>
public class FileStorageListItemDto : BasicAppDto
{
    /// <summary>
    /// 文件主键
    /// </summary>
    public long FileId { get; set; }

    /// <summary>
    /// 存储类型
    /// </summary>
    public FileStorageType StorageType { get; set; }

    /// <summary>
    /// 存储提供商
    /// </summary>
    public string? StorageProvider { get; set; }

    /// <summary>
    /// 存储区域
    /// </summary>
    public string? StorageRegion { get; set; }

    /// <summary>
    /// 是否为主存储
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// 是否为备份存储
    /// </summary>
    public bool IsBackup { get; set; }

    /// <summary>
    /// 是否启用 CDN
    /// </summary>
    public bool EnableCdn { get; set; }

    /// <summary>
    /// 是否压缩存储
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    /// 压缩率
    /// </summary>
    public decimal? CompressionRatio { get; set; }

    /// <summary>
    /// 存储状态
    /// </summary>
    public FileStorageStatus Status { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTimeOffset? UploadedAt { get; set; }

    /// <summary>
    /// 上传耗时
    /// </summary>
    public long? UploadDuration { get; set; }

    /// <summary>
    /// 重试次数
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 最后验证时间
    /// </summary>
    public DateTimeOffset? LastVerifiedAt { get; set; }

    /// <summary>
    /// 是否已验证
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// 是否已同步
    /// </summary>
    public bool IsSynced { get; set; }

    /// <summary>
    /// 同步时间
    /// </summary>
    public DateTimeOffset? SyncedAt { get; set; }

    /// <summary>
    /// 同步源存储主键
    /// </summary>
    public long? SyncSourceId { get; set; }

    /// <summary>
    /// 访问控制
    /// </summary>
    public string? AccessControl { get; set; }

    /// <summary>
    /// 存储类别
    /// </summary>
    public string? StorageClass { get; set; }

    /// <summary>
    /// 缓存控制
    /// </summary>
    public string? CacheControl { get; set; }

    /// <summary>
    /// 排序号
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否包含存储位置
    /// </summary>
    public bool HasLocation { get; set; }

    /// <summary>
    /// 是否包含内网链接
    /// </summary>
    public bool HasPrivateLink { get; set; }

    /// <summary>
    /// 是否包含公网链接
    /// </summary>
    public bool HasPublicLink { get; set; }

    /// <summary>
    /// 是否包含 CDN 链接
    /// </summary>
    public bool HasCdnLink { get; set; }

    /// <summary>
    /// 是否包含失败明细
    /// </summary>
    public bool HasFailureDetail { get; set; }

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
