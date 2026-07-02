#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasTelegramConversationStateCacheItem
// Guid:7d1c5f28-3a94-4e6b-8f07-2b5e9d4a1c63
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/03 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// Telegram 会话多步交互状态缓存项。
/// </summary>
/// <remarks>
/// 与框架 <c>ConversationState</c> 一一映射（机器人为平台级资产，忽略多租户键段）；
/// 键 = botName:chatId:userId（见 <see cref="SaasCacheKeys.TelegramConversationState"/>）。
/// </remarks>
[IgnoreMultiTenancy]
[CacheName(SaasCacheNames.TelegramConversationState)]
public sealed class SaasTelegramConversationStateCacheItem
{
    /// <summary>
    /// 当前步骤标识（由业务处理器定义，如 awaiting_amount）。
    /// </summary>
    public string Step { get; set; } = string.Empty;

    /// <summary>
    /// 上下文数据 JSON（由业务处理器写入，下一步处理器解析）。
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// 状态创建时间。
    /// </summary>
    public DateTimeOffset CreateTime { get; set; } = DateTimeOffset.UtcNow;
}
