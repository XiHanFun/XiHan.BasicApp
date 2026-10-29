// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Notification.Domain.Entities;
using XiHan.BasicApp.Notification.Domain.Services;

namespace XiHan.BasicApp.Notification.Operations.TelegramBots;

/// <summary>
/// 一个 Telegram 机器人（令牌不回显）。
/// </summary>
/// <param name="Id">机器人 ID。</param>
/// <param name="Name">名称。</param>
/// <param name="HasToken">是否存了令牌。</param>
/// <param name="AdminUsers">管理员用户 ID（逗号分隔）。</param>
/// <param name="AllowedGroupChatIds">群组白名单（逗号分隔）。</param>
/// <param name="AllowedCommands">命令白名单（逗号分隔）。</param>
/// <param name="EnableFallbackReply">是否兜底回复。</param>
/// <param name="BroadcastChatIds">广播群（逗号分隔）。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record TelegramBotResponse(Guid Id, string Name, bool HasToken, string? AdminUsers, string? AllowedGroupChatIds, string? AllowedCommands, bool EnableFallbackReply, string? BroadcastChatIds, bool IsEnabled, int Order, string? Remark, DateTimeOffset CreatedAt, DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影。
    /// </summary>
    /// <param name="b">机器人。</param>
    /// <returns>响应。</returns>
    public static TelegramBotResponse From(SysTelegramBot b)
    {
        ArgumentNullException.ThrowIfNull(b);
        return new(b.Id, b.Name, !string.IsNullOrEmpty(b.Token), b.AdminUsers, b.AllowedGroupChatIds, b.AllowedCommands, b.EnableFallbackReply, b.BroadcastChatIds, b.IsEnabled, b.Order, b.Remark, b.CreatedAt, b.ModifiedAt);
    }
}

/// <summary>
/// <c>telegram-bots.create</c> 的请求。
/// </summary>
/// <param name="Name">名称（唯一）。</param>
/// <param name="Token">令牌（形如 <c>123456:ABC…</c>）。</param>
/// <param name="AdminUsers">管理员用户 ID（逗号分隔）。</param>
/// <param name="AllowedGroupChatIds">群组白名单（逗号分隔）。</param>
/// <param name="AllowedCommands">命令白名单（逗号分隔）。</param>
/// <param name="EnableFallbackReply">是否兜底回复。</param>
/// <param name="BroadcastChatIds">广播群（逗号分隔）。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateTelegramBotRequest(
    [Required, StringLength(64, MinimumLength = 1)] string Name,
    [Required, StringLength(256, MinimumLength = 1)] string Token,
    [StringLength(500)] string? AdminUsers = null,
    [StringLength(500)] string? AllowedGroupChatIds = null,
    [StringLength(500)] string? AllowedCommands = null,
    bool EnableFallbackReply = false,
    [StringLength(500)] string? BroadcastChatIds = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public TelegramBotDraft ToDraft() => new(Name, Token, AdminUsers, AllowedGroupChatIds, AllowedCommands, EnableFallbackReply, BroadcastChatIds, IsEnabled, Order, Remark);
}

/// <summary>
/// <c>telegram-bots.update</c> 的请求（令牌不填即保留）。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">机器人 ID。</param>
/// <param name="Name">名称（唯一）。</param>
/// <param name="Token">令牌。</param>
/// <param name="AdminUsers">管理员用户 ID（逗号分隔）。</param>
/// <param name="AllowedGroupChatIds">群组白名单（逗号分隔）。</param>
/// <param name="AllowedCommands">命令白名单（逗号分隔）。</param>
/// <param name="EnableFallbackReply">是否兜底回复。</param>
/// <param name="BroadcastChatIds">广播群（逗号分隔）。</param>
/// <param name="IsEnabled">是否启用。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateTelegramBotRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string Name,
    [StringLength(256)] string? Token = null,
    [StringLength(500)] string? AdminUsers = null,
    [StringLength(500)] string? AllowedGroupChatIds = null,
    [StringLength(500)] string? AllowedCommands = null,
    bool EnableFallbackReply = false,
    [StringLength(500)] string? BroadcastChatIds = null,
    bool IsEnabled = true,
    [Range(0, 100_000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成内容。
    /// </summary>
    /// <returns>内容。</returns>
    public TelegramBotDraft ToDraft() => new(Name, Token, AdminUsers, AllowedGroupChatIds, AllowedCommands, EnableFallbackReply, BroadcastChatIds, IsEnabled, Order, Remark);
}
