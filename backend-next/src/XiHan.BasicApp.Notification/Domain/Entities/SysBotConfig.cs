// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// 群机器人配置（平台库实体，随所属侧）：钉钉、飞书或企业微信的 Webhook 地址、签名密钥与安全关键词。
/// 每个服务商在一侧有一个缺省机器人，通知按机器人渠道发到本侧每个服务商的缺省机器人；租户没有就不发（不借平台的机器人，4.x 同）。
/// 签名密钥加密存储，响应里不回显。
/// </summary>
public sealed class SysBotConfig : AppAuditedEntity, IOwnedByTenant
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
    public BotProvider Provider { get; set; } = BotProvider.DingTalk;

    /// <summary>
    /// 完整的 Webhook 地址（钉钉带 <c>access_token</c>、飞书带机器人令牌、企业微信带 <c>key</c>）。
    /// </summary>
    [StringLength(500)]
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// 签名密钥（密文；钉钉加签、飞书签名校验，企业微信不用）。
    /// </summary>
    [StringLength(1024)]
    public string? Secret { get; set; }

    /// <summary>
    /// 安全关键词（钉钉、飞书要求消息里含有它，发送时放在第一行）。
    /// </summary>
    [StringLength(100)]
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否本侧这个服务商的缺省机器人。
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
