// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 短信配置（平台库实体，随所属侧）。
/// </summary>
/// <remarks>
/// 服务商凭据、签名与模板映射（本应用的短信模板编码 → 服务商的模板 ID）。
/// 发送用当前一侧启用的缺省配置，租户没有就用平台的；访问密钥加密存储，响应里不回显。
/// </remarks>
public sealed class SysSmsConfig : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧唯一）。
    /// </summary>
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 服务商。
    /// </summary>
    public SmsProvider Provider { get; set; } = SmsProvider.Aliyun;

    /// <summary>
    /// 访问密钥 ID（阿里云 AccessKeyId、腾讯云 SecretId）。
    /// </summary>
    [StringLength(256)]
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥（密文）。
    /// </summary>
    [StringLength(1024)]
    public string? AccessKeySecret { get; set; }

    /// <summary>
    /// 应用 ID（腾讯云 SmsSdkAppId）。
    /// </summary>
    [StringLength(64)]
    public string? SdkAppId { get; set; }

    /// <summary>
    /// 签名。
    /// </summary>
    [StringLength(64)]
    public string SignName { get; set; } = string.Empty;

    /// <summary>
    /// 地域（腾讯云如 <c>ap-guangzhou</c>）。
    /// </summary>
    [StringLength(64)]
    public string? Region { get; set; }

    /// <summary>
    /// 模板映射（JSON 对象：本应用的模板编码 → 服务商的模板 ID）。
    /// </summary>
    [LongText]
    public string? TemplateMap { get; set; }

    /// <summary>
    /// 是否本侧的缺省配置。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsEnabled { get; set; } = true;

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
