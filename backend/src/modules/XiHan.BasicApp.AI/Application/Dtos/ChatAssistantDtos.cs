// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#pragma warning disable CS1591

namespace XiHan.BasicApp.AI.Application.Dtos;

/// <summary>
/// 打开助手会话 DTO
/// </summary>
public sealed class ChatAssistantOpenDto
{
    public long AssistantId { get; set; }
}

/// <summary>
/// 助手会话 DTO
/// </summary>
/// <remarks>会话主键显式命名为 ConversationId，与 Saas 侧 ChatConversationDto 一致。</remarks>
public sealed class ChatAssistantConversationDto
{
    public long ConversationId { get; set; }
    public long AssistantId { get; set; }
    public string AssistantName { get; set; } = string.Empty;
    public string? Avatar { get; set; }

    /// <summary>本次调用是否新建了会话（前端据此决定是否刷新会话列表）</summary>
    public bool Created { get; set; }
}

/// <summary>
/// 请求助手回复 DTO
/// </summary>
/// <remarks>ReplyId 由前端生成，增量推送按它归位到同一条占位气泡。</remarks>
public sealed class ChatAssistantReplyDto
{
    public long ConversationId { get; set; }
    public string ReplyId { get; set; } = string.Empty;
}

/// <summary>
/// 助手回复结果 DTO（MessageId 为空表示失败，Error 给出原因）
/// </summary>
public sealed class ChatAssistantReplyResultDto
{
    public long? MessageId { get; set; }
    public string? Error { get; set; }
}
