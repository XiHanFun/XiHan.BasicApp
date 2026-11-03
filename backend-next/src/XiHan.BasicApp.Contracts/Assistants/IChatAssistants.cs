// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Assistants;

/// <summary>
/// 聊天里可用的助手
/// </summary>
/// <param name="Id">助手 ID</param>
/// <param name="Code">编码</param>
/// <param name="Name">名称</param>
/// <param name="Avatar">头像地址</param>
/// <param name="Description">简介</param>
/// <param name="Greeting">开场白</param>
/// <param name="IsDefault">是否缺省助手</param>
/// <param name="HistoryRounds">带入上下文的历史消息条数</param>
public sealed record ChatAssistantInfo(Guid Id, string Code, string Name, string? Avatar, string? Description, string? Greeting, bool IsDefault, int HistoryRounds);

/// <summary>
/// 助手上下文里一条消息的角色
/// </summary>
public enum ChatAssistantRole
{
    /// <summary>
    /// 用户的提问
    /// </summary>
    User = 0,

    /// <summary>
    /// 助手的回复
    /// </summary>
    Assistant = 1,
}

/// <summary>
/// 助手上下文里的一条消息
/// </summary>
/// <param name="Role">角色</param>
/// <param name="Text">正文</param>
public sealed record ChatAssistantTurn(ChatAssistantRole Role, string Text);

/// <summary>
/// 聊天的助手应答（AI 实现）
/// </summary>
/// <remarks>
/// 聊天不引用 AI：AI 模块在 Feature 里登记实现，聊天以 <c>IEnumerable&lt;IChatAssistants&gt;</c> 注入；AI 关掉时没有实现，聊天的助手操作明确返回「助手不可用」，其余功能照常
/// </remarks>
public interface IChatAssistants
{
    /// <summary>
    /// 启用中的助手
    /// </summary>
    /// <param name="context">调用上下文</param>
    ValueTask<Result<IReadOnlyList<ChatAssistantInfo>>> ListAvailableAsync(InvocationContext context);

    /// <summary>
    /// 取一个启用中的助手
    /// </summary>
    /// <param name="context">调用上下文</param>
    /// <param name="assistantId">助手 ID</param>
    /// <returns>助手；不存在或已停用为失败</returns>
    ValueTask<Result<ChatAssistantInfo>> FindAsync(InvocationContext context, Guid assistantId);

    /// <summary>
    /// 流式生成一轮回复
    /// </summary>
    /// <remarks>
    /// 工具与知识检索都以调用方身份、在调用方一侧执行。开始之前的失败（提示词停用、检索失败、提供商未配置）作为结果返回；开始之后模型出错由枚举抛出
    /// </remarks>
    /// <param name="context">调用上下文</param>
    /// <param name="assistantId">助手 ID</param>
    /// <param name="turns">上下文，按时间从早到晚；最后一条用户消息是本轮提问</param>
    /// <returns>增量文本</returns>
    ValueTask<Result<IAsyncEnumerable<string>>> StreamReplyAsync(InvocationContext context, Guid assistantId, IReadOnlyList<ChatAssistantTurn> turns);
}
