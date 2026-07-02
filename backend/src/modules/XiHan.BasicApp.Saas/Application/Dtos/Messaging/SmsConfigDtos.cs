#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsConfigDtos
// Guid:9d5c7e23-1b48-4f06-a9d3-6e8f2a4c7b50
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 短信配置分页查询 DTO
/// </summary>
public sealed class SmsConfigPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（配置编码、名称、签名、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 短信服务商
    /// </summary>
    public SmsProviderType? Provider { get; set; }

    /// <summary>
    /// 是否默认网关
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// 短信配置创建 DTO
/// </summary>
public sealed class SmsConfigCreateDto : BasicAppCDto
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
    /// 短信服务商
    /// </summary>
    public SmsProviderType Provider { get; set; } = SmsProviderType.Aliyun;

    /// <summary>
    /// 访问密钥ID（阿里云 AccessKeyId / 腾讯云 SecretId）
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥（敏感字段，仅写入）
    /// </summary>
    public string AccessKeySecret { get; set; } = string.Empty;

    /// <summary>
    /// 应用ID（腾讯云 SmsSdkAppId，腾讯云必填；阿里云不使用）
    /// </summary>
    public string? SdkAppId { get; set; }

    /// <summary>
    /// 短信签名（服务商控制台审核通过的签名名称）
    /// </summary>
    public string SignName { get; set; } = string.Empty;

    /// <summary>
    /// 地域（腾讯云必填，如 ap-guangzhou；阿里云不使用）
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 模板映射（JSON：内部模板码 → 服务商模板码 + 参数序）
    /// </summary>
    public string? TemplateMap { get; set; }

    /// <summary>
    /// 是否默认网关
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
/// 短信配置更新 DTO
/// </summary>
public sealed class SmsConfigUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 配置名称
    /// </summary>
    public string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 短信服务商
    /// </summary>
    public SmsProviderType Provider { get; set; } = SmsProviderType.Aliyun;

    /// <summary>
    /// 访问密钥ID（阿里云 AccessKeyId / 腾讯云 SecretId）
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥（为空表示保留原密钥）
    /// </summary>
    public string? AccessKeySecret { get; set; }

    /// <summary>
    /// 应用ID（腾讯云 SmsSdkAppId，腾讯云必填；阿里云不使用）
    /// </summary>
    public string? SdkAppId { get; set; }

    /// <summary>
    /// 短信签名（服务商控制台审核通过的签名名称）
    /// </summary>
    public string SignName { get; set; } = string.Empty;

    /// <summary>
    /// 地域（腾讯云必填，如 ap-guangzhou；阿里云不使用）
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 模板映射（JSON：内部模板码 → 服务商模板码 + 参数序）
    /// </summary>
    public string? TemplateMap { get; set; }

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
/// 短信配置状态更新 DTO
/// </summary>
public sealed class SmsConfigStatusUpdateDto
{
    /// <summary>
    /// 短信配置主键
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 短信配置默认更新 DTO
/// </summary>
public sealed class SmsConfigDefaultUpdateDto
{
    /// <summary>
    /// 短信配置主键
    /// </summary>
    public long BasicId { get; set; }
}

/// <summary>
/// 短信配置列表项 DTO
/// </summary>
/// <remarks>AccessKeySecret 为敏感字段不下发，仅以 HasAccessKeySecret 标识是否已配置</remarks>
public class SmsConfigListItemDto : BasicAppDto
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
    /// 短信服务商
    /// </summary>
    public SmsProviderType Provider { get; set; }

    /// <summary>
    /// 短信签名
    /// </summary>
    public string SignName { get; set; } = string.Empty;

    /// <summary>
    /// 应用ID（腾讯云）
    /// </summary>
    public string? SdkAppId { get; set; }

    /// <summary>
    /// 地域（腾讯云）
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 是否已配置访问密钥
    /// </summary>
    public bool HasAccessKeySecret { get; set; }

    /// <summary>
    /// 是否默认网关
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
/// 短信配置详情 DTO
/// </summary>
/// <remarks>AccessKeySecret 为敏感字段不下发，仅以 HasAccessKeySecret 标识是否已配置</remarks>
public sealed class SmsConfigDetailDto : SmsConfigListItemDto
{
    /// <summary>
    /// 访问密钥ID
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// 模板映射（JSON）
    /// </summary>
    public string? TemplateMap { get; set; }

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
