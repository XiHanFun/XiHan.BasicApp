// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 邮件配置（平台库实体，随所属侧）。
/// </summary>
/// <remarks>
/// SMTP 服务器与发件人。发送用当前一侧启用的缺省配置，租户没有就用平台的（4.x 不回落平台）。
/// 密码加密存储，响应里不回显，修改时不填即保留原值。
/// </remarks>
public sealed class SysEmailConfig : AppAuditedEntity, IOwnedByTenant
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
    /// SMTP 主机。
    /// </summary>
    [StringLength(256)]
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 端口（缺省 587）。
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// 加密连接：465 端口一连上即 TLS，其余端口用 STARTTLS。
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// 接受无效证书（只用于内网测试）。
    /// </summary>
    public bool AcceptInvalidCertificate { get; set; }

    /// <summary>
    /// 发件地址。
    /// </summary>
    [StringLength(256)]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人名称。
    /// </summary>
    [StringLength(128)]
    public string? FromName { get; set; }

    /// <summary>
    /// 登录名。
    /// </summary>
    /// <remarks>
    /// 为空即不认证。
    /// </remarks>
    [StringLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// 密码（密文）。
    /// </summary>
    [StringLength(1024)]
    public string? Password { get; set; }

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
