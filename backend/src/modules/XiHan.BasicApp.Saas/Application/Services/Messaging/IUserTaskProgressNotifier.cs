#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserTaskProgressNotifier
// Guid:b5e8c2f7-4a91-4d36-8c0e-6f3a9d5b2e74
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 用户后台任务进度推送服务
/// </summary>
/// <remarks>
/// 经实时通道（SignalR TaskProgress 事件）把服务端任务的进行中/进度/终态推给指定用户，
/// 前端灵动岛订阅该事件呈现常驻后台任务。推送失败静默（不影响业务主流程）。
/// </remarks>
public interface IUserTaskProgressNotifier
{
    /// <summary>
    /// 推送任务进行中
    /// </summary>
    /// <param name="userId">接收用户</param>
    /// <param name="taskId">任务标识（同 id 复用同一条灵动岛任务）</param>
    /// <param name="label">任务文案</param>
    /// <param name="detail">副文本（可空）</param>
    /// <param name="progress">进度 0-100（空=不确定态）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task NotifyRunningAsync(long userId, string taskId, string label, string? detail = null, int? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推送任务成功
    /// </summary>
    /// <param name="userId">接收用户</param>
    /// <param name="taskId">任务标识</param>
    /// <param name="label">任务文案</param>
    /// <param name="detail">副文本（可空）</param>
    /// <param name="link">点击跳转链接（可空）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task NotifySucceededAsync(long userId, string taskId, string label, string? detail = null, string? link = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 推送任务失败
    /// </summary>
    /// <param name="userId">接收用户</param>
    /// <param name="taskId">任务标识</param>
    /// <param name="label">任务文案</param>
    /// <param name="detail">副文本（可空）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task NotifyFailedAsync(long userId, string taskId, string label, string? detail = null, CancellationToken cancellationToken = default);
}
