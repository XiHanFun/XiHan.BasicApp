// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Notification;

/// <summary>
/// 站内待办投递（Notification 实现）。
/// </summary>
/// <remarks>
/// 别的包给一组用户发一条「待办」类站内信（带链接，按收件人的站内偏好过滤，提交之后实时推送），用于审批待办、抄送、流程故障这类提醒；
/// 投在调用所在的一侧，不经发件人权限判定。没有实现（Notification 没装）时调用方自己决定怎么处理。
/// </remarks>
public interface IInboxDelivery
{
    /// <summary>
    /// 投递。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="message">内容。</param>
    /// <returns>实际送达的人数。</returns>
    Task<Result<int>> DeliverAsync(InvocationContext context, InboxMessage message);
}

/// <summary>
/// 一条待办类站内信。
/// </summary>
/// <param name="UserIds">收件人。</param>
/// <param name="Title">标题。</param>
/// <param name="Content">正文（纯文本）。</param>
/// <param name="Link">前端路由，收件人点开时跳过去。</param>
/// <param name="BusinessType">业务类型（如 <c>workflow</c>）。</param>
/// <param name="BusinessId">业务 ID（如流程实例 ID）。</param>
public sealed record InboxMessage(IReadOnlyCollection<Guid> UserIds, string Title, string? Content, string? Link, string BusinessType, string BusinessId);
