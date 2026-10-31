// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 存储配置：本地目录或 S3 兼容的对象存储。
/// </summary>
/// <remarks>
/// 平台库实体，随所属侧，方案 5.12。S3、OSS、COS、MinIO 都走 S3 协议。
/// 上传用当前一侧的缺省配置，租户没有就用平台的缺省配置；已有副本按它记下的配置读写。访问密钥加密存储，响应里不回显。
/// </remarks>
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code))]
public sealed class SysStorageConfig : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧唯一，不含空白）。
    /// </summary>
    [StringLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 存储类型。
    /// </summary>
    public StorageType Type { get; set; }

    /// <summary>
    /// 服务地址（S3 兼容存储必填）。
    /// </summary>
    /// <remarks>
    /// 如 <c>https://oss-cn-hangzhou.aliyuncs.com</c>；本地存储为空。
    /// </remarks>
    [StringLength(500)]
    public string? Endpoint { get; set; }

    /// <summary>
    /// 区域（S3 兼容存储，缺省 <c>us-east-1</c>）。
    /// </summary>
    [StringLength(100)]
    public string? Region { get; set; }

    /// <summary>
    /// 桶名。
    /// </summary>
    /// <remarks>
    /// 本地存储是根目录下的子目录。小写字母、数字与连字符，3 到 63 个字符。
    /// </remarks>
    [StringLength(63)]
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥 ID（S3 兼容存储必填）。
    /// </summary>
    [StringLength(200)]
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// 访问密钥（加密存储，S3 兼容存储必填）。
    /// </summary>
    [StringLength(1000)]
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// 是否这一侧的缺省配置（每侧至多一条，须启用）。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public StorageConfigStatus Status { get; set; }

    /// <summary>
    /// 排序。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 存储类型：本地目录，或经 S3 协议访问的对象存储。
/// </summary>
/// <remarks>
/// 4.x 同。
/// </remarks>
public enum StorageType
{
    /// <summary>
    /// 本地目录。
    /// </summary>
    /// <remarks>
    /// 根目录取配置 <c>BasicApp:Files:LocalRootPath</c>。
    /// </remarks>
    Local = 0,

    /// <summary>
    /// Amazon S3 或其他 S3 兼容存储（路径式访问）。
    /// </summary>
    S3 = 1,

    /// <summary>
    /// 阿里云 OSS（S3 兼容接口，虚拟主机式访问）。
    /// </summary>
    Oss = 2,

    /// <summary>
    /// 腾讯云 COS（S3 兼容接口，虚拟主机式访问）。
    /// </summary>
    Cos = 3,

    /// <summary>
    /// MinIO（路径式访问）。
    /// </summary>
    MinIO = 4,
}

/// <summary>
/// 存储配置状态。
/// </summary>
public enum StorageConfigStatus
{
    /// <summary>
    /// 启用。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：不再接收新上传，已有副本照常读取。
    /// </summary>
    Disabled = 1,
}
