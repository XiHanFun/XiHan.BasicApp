// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 邮件配置（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 落平台库、按 <c>OwnerTenantId</c> 分侧；发送只认本侧启用的缺省配置，租户没有就用平台的，由平台代发
/// 每侧至多一条缺省，第一条自动成为缺省，缺省的须启用、不能停用或删除：免得这一侧没有可用的配置
/// 配置到发送时才取，不定格在发送记录上：改配置或换缺省对排队中与重试中的邮件立即生效
/// 密码加密存储、不回显，修改时不填即保留；但改了主机或端口、关掉加密或改成接受无效证书时要重新填，免得保存的密码被送到别的服务器或不安全的连接上
/// 编码同侧唯一用部分唯一索引，删除后同编码可重建
/// </remarks>
[SugarTable(TableName = "sys_email_config", TableDescription = "邮件配置（平台库实体，随所属侧）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code), Name = "ux_sys_email_config_owner_tenant_id_code")]
public sealed class SysEmailConfig : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧唯一）
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 64, IsNullable = false, ColumnDescription = "编码（同一侧唯一）")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 128, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 主机
    /// </summary>
    [SugarColumn(ColumnName = "smtp_host", Length = 256, IsNullable = false, ColumnDescription = "SMTP 主机")]
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 端口（缺省 587）
    /// </summary>
    [SugarColumn(ColumnName = "smtp_port", IsNullable = false, ColumnDescription = "SMTP 端口（缺省 587）")]
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// 加密连接：465 端口一连上即 TLS，其余端口用 STARTTLS
    /// </summary>
    [SugarColumn(ColumnName = "use_ssl", IsNullable = false, ColumnDescription = "加密连接：465 端口一连上即 TLS，其余端口用 STARTTLS")]
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// 接受无效证书（只用于内网测试）
    /// </summary>
    [SugarColumn(ColumnName = "accept_invalid_certificate", IsNullable = false, ColumnDescription = "接受无效证书（只用于内网测试）")]
    public bool AcceptInvalidCertificate { get; set; }

    /// <summary>
    /// 发件地址
    /// </summary>
    [SugarColumn(ColumnName = "from_email", Length = 256, IsNullable = false, ColumnDescription = "发件地址")]
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 发件人名称
    /// </summary>
    [SugarColumn(ColumnName = "from_name", Length = 128, IsNullable = true, ColumnDescription = "发件人名称")]
    public string? FromName { get; set; }

    /// <summary>
    /// 登录名
    /// </summary>
    /// <remarks>
    /// 为空即不认证
    /// </remarks>
    [SugarColumn(ColumnName = "user_name", Length = 256, IsNullable = true, ColumnDescription = "登录名")]
    public string? UserName { get; set; }

    /// <summary>
    /// 密码（密文）
    /// </summary>
    [SugarColumn(ColumnName = "password", Length = 1024, IsNullable = true, ColumnDescription = "密码（密文）")]
    public string? Password { get; set; }

    /// <summary>
    /// 是否本侧的缺省配置
    /// </summary>
    [SugarColumn(ColumnName = "is_default", IsNullable = false, ColumnDescription = "是否本侧的缺省配置")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false, ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
