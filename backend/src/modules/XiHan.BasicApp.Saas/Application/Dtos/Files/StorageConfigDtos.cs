// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 存储配置分页查询 DTO
/// </summary>
public sealed class StorageConfigPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（配置编码、名称、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 存储类型
    /// </summary>
    public StorageConfigType? StorageType { get; set; }

    /// <summary>
    /// 是否默认存储
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// 存储配置创建 DTO
/// </summary>
public sealed class StorageConfigCreateDto : BasicAppCDto
{
    /// <summary>
    /// 配置编码（租户内唯一）
    /// </summary>
    public string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 存储类型
    /// </summary>
    public StorageConfigType StorageType { get; set; } = StorageConfigType.Local;

    /// <summary>
    /// 端点URL（S3兼容接口地址）
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// 区域/地域
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 存储桶名称（Local 类型时存放根路径）
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 访问密钥ID
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// 访问密钥（敏感字段，仅写入）
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// 是否默认存储
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 存储配置更新 DTO
/// </summary>
public sealed class StorageConfigUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 存储类型
    /// </summary>
    public StorageConfigType StorageType { get; set; } = StorageConfigType.Local;

    /// <summary>
    /// 端点URL（S3兼容接口地址）
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// 区域/地域
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 存储桶名称（Local 类型时存放根路径）
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 访问密钥ID
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// 访问密钥（为空表示保留原密钥）
    /// </summary>
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 存储配置状态更新 DTO
/// </summary>
public sealed class StorageConfigStatusUpdateDto
{
    /// <summary>
    /// 存储配置主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 存储配置默认更新 DTO
/// </summary>
public sealed class StorageConfigDefaultUpdateDto
{
    /// <summary>
    /// 存储配置主键
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// 存储配置列表项 DTO
/// </summary>
public class StorageConfigListItemDto : BasicAppDto
{
    /// <summary>
    /// 配置编码
    /// </summary>
    public string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 存储类型
    /// </summary>
    public StorageConfigType StorageType { get; set; }

    /// <summary>
    /// 端点URL
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// 区域/地域
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 存储桶名称（Local 类型时为根路径）
    /// </summary>
    public string? BucketName { get; set; }

    /// <summary>
    /// 是否默认存储
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 存储配置详情 DTO
/// </summary>
/// <remarks>SecretAccessKey 为敏感字段不下发，仅以 HasSecretAccessKey 标识是否已配置</remarks>
public sealed class StorageConfigDetailDto : StorageConfigListItemDto
{
    /// <summary>
    /// 访问密钥ID
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// 是否已配置访问密钥
    /// </summary>
    public bool HasSecretAccessKey { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建人主键
    /// </summary>
    public long? CreatedId { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 修改人主键
    /// </summary>
    public long? ModifiedId { get; set; }

    /// <summary>
    /// 修改人
    /// </summary>
    public string? ModifiedBy { get; set; }
}
