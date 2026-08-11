// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Chat.Domain.Configurations;

/// <summary>
/// 在线聊天配置键常量（全局配置，TenantId=0）
/// </summary>
public static class ChatConfigKeys
{
    /// <summary>
    /// 配置分组
    /// </summary>
    public const string Group = "chat";

    /// <summary>
    /// 聊天消息保留天数（清理任务按此物理删除过期消息）
    /// </summary>
    public const string RetentionDays = "chat:retention-days";

    /// <summary>
    /// 聊天敏感词词库（换行/中英文逗号/分号分隔，空=关闭拦截）
    /// </summary>
    public const string SensitiveWords = "chat:sensitive-words";
}
