// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统机器人配置实体（Webhook 型：钉钉/飞书/企业微信）
/// 承载群机器人 Webhook 地址与签名秘钥，驱动框架 Bot 提供者运行时构建客户端
/// </summary>
/// <remarks>
/// 写入：
/// - ConfigCode 在租户内必须唯一（UX_TeId_CoCd）
/// - 默认互斥按 Provider 维度：同一租户同一服务商下有且仅有一条 IsDefault=true；服务层必须保证互斥
/// - Secret 为敏感字段（钉钉加签秘钥/飞书签名秘钥），Data Protection 加密落库，读侧永不回显；
///   企业微信的 key 在 WebhookUrl 里，Secret 可空
///
/// 查询：
/// - 获取某服务商默认机器人：WHERE Provider=x AND IsDefault=true AND IsEnabled=true
/// - 框架 store 按此读默认行并映射为各家 Options（镜像短信/邮件配置范式）
///
/// 场景：
/// - 运维告警、业务通知推送到钉钉/飞书/企微群
/// </remarks>
[SugarTable(TableName = "Sys_Bot_Config", TableDescription = "系统机器人配置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_CoCd", nameof(TenantId), OrderByType.Asc, nameof(ConfigCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_Prov", nameof(TenantId), OrderByType.Asc, nameof(Provider), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_Prov_IsDe_IsEn", nameof(TenantId), OrderByType.Asc, nameof(Provider), OrderByType.Asc, nameof(IsDefault), OrderByType.Desc, nameof(IsEnabled), OrderByType.Asc)]
public partial class SysBotConfig : BasicAppFullAuditedEntity
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
    /// 机器人服务商
    /// </summary>
    [SugarColumn(ColumnName = "Provider", ColumnDescription = "机器人服务商")]
    public virtual BotProviderType Provider { get; set; } = BotProviderType.DingTalk;

    /// <summary>
    /// Webhook 地址（含凭证的完整地址：钉钉 ?access_token=、飞书 /hook/{token}、企微 ?key=）
    /// </summary>
    [SugarColumn(ColumnName = "Webhook_Url", ColumnDescription = "Webhook地址", Length = 500, IsNullable = false)]
    public virtual string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// 签名秘钥（钉钉加签秘钥/飞书签名秘钥；企业微信不使用，key 在 WebhookUrl 中；敏感字段，加密落库，读侧不回显）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Secret", ColumnDescription = "签名秘钥", Length = 500, IsNullable = true)]
    public virtual string? Secret { get; set; }

    /// <summary>
    /// 安全关键词（钉钉/飞书自定义关键词安全设置；企业微信不使用）
    /// </summary>
    [SugarColumn(ColumnName = "Keyword", ColumnDescription = "安全关键词", Length = 100, IsNullable = true)]
    public virtual string? Keyword { get; set; }

    /// <summary>
    /// 是否默认机器人（同一租户同一服务商下有且仅有一条为true）
    /// </summary>
    [SugarColumn(ColumnName = "Is_Default", ColumnDescription = "是否默认机器人")]
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
