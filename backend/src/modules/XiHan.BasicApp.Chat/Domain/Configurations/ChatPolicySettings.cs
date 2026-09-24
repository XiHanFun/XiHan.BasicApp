// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Chat.Domain.Configurations;

/// <summary>
/// 聊天策略（配置键 <see cref="ChatConfigKeys.Policy"/>）
/// </summary>
public sealed class ChatPolicySettings
{
    /// <summary>
    /// 消息保留天数（清理任务物理删除更早的消息，必须为正整数）
    /// </summary>
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    /// 敏感词（空表示不拦截；发送、编辑文本时按不区分大小写的包含匹配拦截）
    /// </summary>
    public List<string> SensitiveWords { get; set; } = [];
}
