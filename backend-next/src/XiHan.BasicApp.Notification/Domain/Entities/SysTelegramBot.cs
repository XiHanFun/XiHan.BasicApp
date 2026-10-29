// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Notification.Domain.Entities;

/// <summary>
/// Telegram 机器人（平台库实体，平台托管，4.x 同：令牌、Webhook 与会话都是平台级的，租户看不到）。
/// 令牌加密存储、响应里不回显；管理员、群组白名单与命令白名单供交互（Webhook 与命令）使用；广播群收平台一侧按机器人渠道发出的通知。
/// </summary>
public sealed class SysTelegramBot : AppAuditedEntity
{
    /// <summary>
    /// 名称（唯一；Webhook 路由按它区分机器人）。
    /// </summary>
    [StringLength(64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 机器人令牌（密文）。
    /// </summary>
    [StringLength(1024)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 管理员的 Telegram 用户 ID（逗号分隔）。
    /// </summary>
    [StringLength(500)]
    public string? AdminUsers { get; set; }

    /// <summary>
    /// 允许的群组与频道 chat ID（逗号分隔；为空即拒收所有群组与频道消息，只有 <c>/start</c> <c>/help</c> <c>/myid</c> 不受限，私聊不受影响；4.x 同）。
    /// </summary>
    [StringLength(500)]
    public string? AllowedGroupChatIds { get; set; }

    /// <summary>
    /// 允许的命令（逗号分隔，不带斜杠；为空即不限）。
    /// </summary>
    [StringLength(500)]
    public string? AllowedCommands { get; set; }

    /// <summary>
    /// 是否对认不出的消息兜底回复。
    /// </summary>
    public bool EnableFallbackReply { get; set; }

    /// <summary>
    /// 广播群的 chat ID（逗号分隔；平台一侧按机器人渠道发的通知发到这些会话，4.x 在配置文件里只能填一个）。
    /// </summary>
    [StringLength(500)]
    public string? BroadcastChatIds { get; set; }

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
