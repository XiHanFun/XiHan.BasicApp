#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BotConfigCommandModels
// Guid:7800b283-3dee-44bc-b7e5-c00b7e1e58d8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 机器人配置创建命令
/// </summary>
/// <remarks>Secret 可空（企业微信的 key 在 WebhookUrl 里，无独立秘钥）</remarks>
public sealed record BotConfigCreateCommand(
    string ConfigCode,
    string ConfigName,
    BotProviderType Provider,
    string WebhookUrl,
    string? Secret,
    string? Keyword,
    bool IsDefault,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// 机器人配置更新命令
/// </summary>
/// <remarks>Secret 为空表示保留原秘钥（前端脱敏不回显）</remarks>
public sealed record BotConfigUpdateCommand(
    long BasicId,
    string ConfigName,
    BotProviderType Provider,
    string WebhookUrl,
    string? Secret,
    string? Keyword,
    int Sort,
    string? Remark);

/// <summary>
/// 机器人配置状态变更命令
/// </summary>
public sealed record BotConfigStatusChangeCommand(long BasicId, bool IsEnabled);

/// <summary>
/// 机器人配置默认变更命令（默认互斥按 Provider 维度）
/// </summary>
public sealed record BotConfigDefaultChangeCommand(long BasicId);

/// <summary>
/// 机器人配置命令结果
/// </summary>
public sealed record BotConfigCommandResult(SysBotConfig Config);
