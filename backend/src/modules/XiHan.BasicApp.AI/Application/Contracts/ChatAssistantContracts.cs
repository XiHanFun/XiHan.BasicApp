// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.AI.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.AI.Application.Contracts;

/// <summary>
/// 聊天助手应用服务接口
/// </summary>
public interface IChatAssistantAppService : IApplicationService
{
    /// <summary>
    /// 打开与指定助手的会话（不存在则创建，并发送开场白）
    /// </summary>
    Task<ChatAssistantConversationDto> OpenConversationAsync(ChatAssistantOpenDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 请求助手回复会话内最后一条用户消息（增量经 SignalR 推送，完成后落库）
    /// </summary>
    Task<ChatAssistantReplyResultDto> ReplyAsync(ChatAssistantReplyDto input, CancellationToken cancellationToken = default);
}
