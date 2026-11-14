// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 群机器人配置（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 落平台库、按 <c>OwnerTenantId</c> 分侧；通知的机器人渠道发到本侧每个服务商的缺省机器人，所以缺省按服务商分：每侧每个服务商至多一个，第一个自动成为缺省，缺省的不能停用、删除或换服务商，免得这个服务商没有可发的
/// 不借平台的机器人：租户的通知不进平台的群，本侧一个都没有即带机器人渠道的发布整次失败；这一点与邮件、短信不同，那两种租户没配时由平台代发
/// Webhook 地址整串加密：钉钉的 <c>access_token</c>、飞书路径里的令牌、企业微信的 <c>key</c> 都在地址里，拿到就能往群里发，所以响应只给主机，修改时交回遮过的地址即保留原值
/// 只认各服务商官方的 https 主机：租户能填的地址不许指向别处
/// 编码同侧唯一用部分唯一索引，删除后同编码可重建
/// </remarks>
[SugarTable(TableName = "sys_bot_config", TableDescription = "群机器人配置（平台库实体，随所属侧）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code), Name = "ux_sys_bot_config_owner_tenant_id_code")]
public sealed class SysBotConfig : AppAuditedEntity, IOwnedByTenant
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
    /// 服务商
    /// </summary>
    [SugarColumn(ColumnName = "provider", IsNullable = false, ColumnDescription = "服务商")]
    public BotProvider Provider { get; set; } = BotProvider.DingTalk;

    /// <summary>
    /// 完整的 Webhook 地址（密文）
    /// </summary>
    /// <remarks>
    /// 钉钉带 <c>access_token</c>、飞书带机器人令牌、企业微信带 <c>key</c>，拿到就能往群里发消息
    /// 请求限 500 字符，全是三字节字符的最长地址加密后约 2100 字符
    /// </remarks>
    [SugarColumn(ColumnName = "webhook_url", Length = 2500, IsNullable = false, ColumnDescription = "完整的 Webhook 地址（密文）")]
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// 签名密钥（密文）
    /// </summary>
    /// <remarks>
    /// 钉钉加签、飞书签名校验，企业微信不用
    /// </remarks>
    [SugarColumn(ColumnName = "secret", Length = 1024, IsNullable = true, ColumnDescription = "签名密钥（密文）")]
    public string? Secret { get; set; }

    /// <summary>
    /// 安全关键词（钉钉、飞书要求消息里含有它，发送时放在第一行）
    /// </summary>
    [SugarColumn(ColumnName = "keyword", Length = 100, IsNullable = true, ColumnDescription = "安全关键词（钉钉、飞书要求消息里含有它，发送时放在第一行）")]
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否本侧这个服务商的缺省机器人
    /// </summary>
    [SugarColumn(ColumnName = "is_default", IsNullable = false, ColumnDescription = "是否本侧这个服务商的缺省机器人")]
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

/// <summary>
/// 群机器人的服务商
/// </summary>
public enum BotProvider
{
    /// <summary>
    /// 钉钉自定义机器人（可选加签与安全关键词）
    /// </summary>
    DingTalk = 1,

    /// <summary>
    /// 飞书自定义机器人（可选签名校验与关键词）
    /// </summary>
    Lark = 2,

    /// <summary>
    /// 企业微信群机器人（地址里带 key，没有签名）
    /// </summary>
    WeCom = 3,
}
