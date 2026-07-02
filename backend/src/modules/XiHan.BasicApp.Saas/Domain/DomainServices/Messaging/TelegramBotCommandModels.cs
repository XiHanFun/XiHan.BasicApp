#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TelegramBotCommandModels
// Guid:611ac6c8-3712-4d0e-820f-9502bc5d8ca7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// Telegram 机器人创建命令
/// </summary>
/// <remarks>AllowedGroupChatIds 为空 = 拒收所有群消息（fail-closed）；AllowedCommands 为空 = 不限制命令</remarks>
public sealed record TelegramBotCreateCommand(
    string BotName,
    string Token,
    string? AdminUsers,
    string? AllowedGroupChatIds,
    string? AllowedCommands,
    bool EnableFallbackReply,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// Telegram 机器人更新命令
/// </summary>
/// <remarks>Token 为空表示保留原 Token（前端脱敏不回显）</remarks>
public sealed record TelegramBotUpdateCommand(
    long BasicId,
    string BotName,
    string? Token,
    string? AdminUsers,
    string? AllowedGroupChatIds,
    string? AllowedCommands,
    bool EnableFallbackReply,
    int Sort,
    string? Remark);

/// <summary>
/// Telegram 机器人状态变更命令
/// </summary>
public sealed record TelegramBotStatusChangeCommand(long BasicId, bool IsEnabled);

/// <summary>
/// Telegram 机器人命令结果
/// </summary>
public sealed record TelegramBotCommandResult(SysTelegramBot Bot);
