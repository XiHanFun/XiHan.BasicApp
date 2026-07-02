#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysEmailConfig
// Guid:9b4e6c83-2a17-4f52-b0d9-7e5c1a8f3d64
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统邮件网关配置实体
/// SMTP 发信配置表：承载 SMTP 服务器连接参数、发件人身份与认证凭证，驱动邮件发送器运行时构建 SMTP 客户端
/// </summary>
/// <remarks>
/// 关联：
/// - SysEmail.FromEmail/FromName 记录发送时实际使用的发件人（信封元数据可覆盖本表默认值）
///
/// 写入：
/// - ConfigCode 在租户内必须唯一（UX_TeId_CoCd）
/// - 同一租户下有且仅有一条 IsDefault=true 的记录；服务层必须保证互斥
/// - Password 为敏感字段，Data Protection 加密落库，读侧永不回显
///
/// 查询：
/// - 获取默认网关：WHERE IsDefault=true AND IsEnabled=true
/// - 发送器每次发送读取默认行，改配置即热生效（镜像短信网关配置范式）
///
/// 场景：
/// - 多 SMTP 服务商切换（自建/企业邮箱/云邮件推送）
/// - 租户自配发件人品牌（FromName 兼作品牌名，TenantId=0 为平台全局配置）
/// </remarks>
[SugarTable(TableName = "Sys_Email_Config", TableDescription = "系统邮件网关配置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_CoCd", nameof(TenantId), OrderByType.Asc, nameof(ConfigCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_IsDe_IsEn", nameof(TenantId), OrderByType.Asc, nameof(IsDefault), OrderByType.Desc, nameof(IsEnabled), OrderByType.Asc)]
public partial class SysEmailConfig : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 配置编码（租户内唯一标识）
    /// </summary>
    [SugarColumn(ColumnName = "Config_Code", ColumnDescription = "配置编码", Length = 100, IsNullable = false)]
    public virtual string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    [SugarColumn(ColumnName = "Config_Name", ColumnDescription = "配置名称", Length = 200, IsNullable = false)]
    public virtual string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 服务器地址
    /// </summary>
    [SugarColumn(ColumnName = "Smtp_Host", ColumnDescription = "SMTP服务器地址", Length = 200, IsNullable = false)]
    public virtual string SmtpHost { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 服务器端口
    /// </summary>
    [SugarColumn(ColumnName = "Smtp_Port", ColumnDescription = "SMTP服务器端口")]
    public virtual int SmtpPort { get; set; } = 587;

    /// <summary>
    /// 是否使用 SSL/TLS
    /// </summary>
    [SugarColumn(ColumnName = "Use_Ssl", ColumnDescription = "是否使用SSL")]
    public virtual bool UseSsl { get; set; } = true;

    /// <summary>
    /// 是否接受无效/自签 TLS 证书（默认 false，生产务必保持 false；仅本地自签 SMTP 联调时置 true）
    /// </summary>
    [SugarColumn(ColumnName = "Accept_Invalid_Certificate", ColumnDescription = "是否接受无效证书")]
    public virtual bool AcceptInvalidCertificate { get; set; } = false;

    /// <summary>
    /// 默认发件邮箱（信封未显式指定 FromEmail 时使用）
    /// </summary>
    [SugarColumn(ColumnName = "From_Email", ColumnDescription = "发件邮箱", Length = 200, IsNullable = false)]
    public virtual string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// 默认发件人显示名（兼作品牌名：注册欢迎/验证码等邮件标题品牌注入）
    /// </summary>
    [SugarColumn(ColumnName = "From_Name", ColumnDescription = "发件人显示名", Length = 200, IsNullable = false)]
    public virtual string FromName { get; set; } = string.Empty;

    /// <summary>
    /// SMTP 认证登录名（多数服务商即发件邮箱；为空则不进行认证）
    /// </summary>
    [SugarColumn(ColumnName = "User_Name", ColumnDescription = "SMTP认证用户名", Length = 200, IsNullable = true)]
    public virtual string? UserName { get; set; }

    /// <summary>
    /// SMTP 认证密码（敏感字段，加密落库，读侧不回显；为空则不进行认证）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Password", ColumnDescription = "SMTP认证密码", Length = 500, IsNullable = true)]
    public virtual string? Password { get; set; }

    /// <summary>
    /// 默认是否使用 HTML 正文
    /// </summary>
    [SugarColumn(ColumnName = "Is_Body_Html", ColumnDescription = "是否HTML正文")]
    public virtual bool IsBodyHtml { get; set; } = true;

    /// <summary>
    /// 是否默认网关（同一租户有且仅有一条为true）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Default", ColumnDescription = "是否默认网关")]
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "Is_Enabled", ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
