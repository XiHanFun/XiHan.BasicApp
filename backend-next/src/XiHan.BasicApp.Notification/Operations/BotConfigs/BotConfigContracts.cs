// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Operations.Messaging;

namespace XiHan.BasicApp.Notification.Operations.BotConfigs;

/// <summary>
/// 一个机器人配置（签名密钥不回显）。
/// </summary>
/// <param name="Id">配置 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Provider">服务商。</param>
/// <param name="WebhookUrl">Webhook 地址。</param>
/// <param name="HasSecret">是否存了签名密钥。</param>
/// <param name="Keyword">安全关键词。</param>
/// <param name="IsDefault">是否本侧这个服务商的缺省。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record BotConfigResponse(Guid Id, string Code, string Name, BotProvider Provider, string WebhookUrl, bool HasSecret, string? Keyword, bool IsDefault, bool IsEnabled, int Order, string? Remark, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="c">配置。</param>
    /// <returns>响应。</returns>
    public static BotConfigResponse From(SysBotConfig c)
    {
        ArgumentNullException.ThrowIfNull(c);
        return new(c.Id, c.Code, c.Name, c.Provider, c.WebhookUrl, c.Secret is not null, c.Keyword, c.IsDefault, c.IsEnabled, c.Order, c.Remark, c.CreatedAt, c.ModifiedAt);
    }
}

/// <summary>
/// <c>bot-configs.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（建好不改）。</param>
/// <param name="Name">名称。</param>
/// <param name="Provider">服务商。</param>
/// <param name="WebhookUrl">Webhook 地址。</param>
/// <param name="Secret">签名密钥。</param>
/// <param name="Keyword">安全关键词。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateBotConfigRequest(
    [Required, StringLength(64, MinimumLength = 1), RegularExpression(MessagingCodes.Pattern)] string Code,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    BotProvider Provider,
    [Required, StringLength(500, MinimumLength = 1)] string WebhookUrl,
    [StringLength(512)] string? Secret = null,
    [StringLength(100)] string? Keyword = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public BotConfigDraft ToDraft() => new(Name, Provider, WebhookUrl, Secret, Keyword, IsEnabled, Order, Remark);
}

/// <summary>
/// <c>bot-configs.update</c> 的请求。
/// </summary>
/// <remarks>
/// 编码不改，签名密钥不填即保留。标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">配置 ID。</param>
/// <param name="Name">名称。</param>
/// <param name="Provider">服务商（缺省的不能换）。</param>
/// <param name="WebhookUrl">Webhook 地址。</param>
/// <param name="Secret">签名密钥。</param>
/// <param name="Keyword">安全关键词。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateBotConfigRequest(
    Guid Id,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    BotProvider Provider,
    [Required, StringLength(500, MinimumLength = 1)] string WebhookUrl,
    [StringLength(512)] string? Secret = null,
    [StringLength(100)] string? Keyword = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public BotConfigDraft ToDraft() => new(Name, Provider, WebhookUrl, Secret, Keyword, IsEnabled, Order, Remark);
}
