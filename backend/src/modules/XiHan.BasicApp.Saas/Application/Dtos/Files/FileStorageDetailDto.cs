// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统文件存储详情 DTO
/// </summary>
public sealed class FileStorageDetailDto : FileStorageListItemDto
{
    /// <summary>
    /// 存储配置主键
    /// </summary>
    public long? StorageConfigId { get; set; }

    /// <summary>
    /// 存储桶名称
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 存储路径
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 完整路径
    /// </summary>
    public string? FullPath { get; set; }

    /// <summary>
    /// 内部访问地址
    /// </summary>
    public string? InternalUrl { get; set; }

    /// <summary>
    /// 外部访问地址
    /// </summary>
    public string? ExternalUrl { get; set; }

    /// <summary>
    /// CDN 地址
    /// </summary>
    public string? CdnUrl { get; set; }

    /// <summary>
    /// 上传失败原因
    /// </summary>
    public string? UploadFailureReason { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 创建者主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改者主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    public string? ModifiedBy { get; set; }
}
