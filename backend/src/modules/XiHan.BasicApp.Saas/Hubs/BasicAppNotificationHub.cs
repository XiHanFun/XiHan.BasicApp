#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BasicAppNotificationHub
// Guid:df3a4b5c-6d7e-4f8a-9b1c-ce5f6a7b8c9d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Web.RealTime.Attributes;
using XiHan.Framework.Web.RealTime.Hubs;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Hubs;

/// <summary>
/// 曦寒基础应用通知 Hub
/// </summary>
/// <remarks>
/// 连接/断开除框架级连接登记（<see cref="IConnectionManager"/>，实时在线判定数据源）外，
/// 同步刷新对应 <see cref="SysUserSession"/> 的最后活动时间，弥补 HTTP 侧无心跳的缺口：
/// 在线用户列表 = 活跃会话（数据库）+ 实时连接标注（连接管理器）。
/// 会话刷新失败只记日志，绝不阻断连接建立/断开。
/// </remarks>
[AuthorizeHub]
public class BasicAppNotificationHub : XiHanHub
{
    private readonly IUserSessionRepository _userSessionRepository;

    private readonly ILogger<BasicAppNotificationHub> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionManager">连接管理器</param>
    /// <param name="userSessionRepository">用户会话仓储</param>
    /// <param name="logger">日志</param>
    public BasicAppNotificationHub(
        IConnectionManager connectionManager,
        IUserSessionRepository userSessionRepository,
        ILogger<BasicAppNotificationHub> logger)
        : base(connectionManager)
    {
        _userSessionRepository = userSessionRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        await TouchSessionActivityAsync();
    }

    /// <inheritdoc />
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        await TouchSessionActivityAsync();
    }

    /// <summary>
    /// 按令牌中的业务会话标识刷新会话最后活动时间（仅活跃会话）
    /// </summary>
    private async Task TouchSessionActivityAsync()
    {
        try
        {
            var sessionId = Context.User?.FindFirst(XiHanClaimTypes.SessionId)?.Value;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            var session = await _userSessionRepository.GetByUserSessionIdAsync(sessionId);
            if (session is null || session.Status != SessionStatus.Active)
            {
                return;
            }

            session.LastActivityTime = DateTimeOffset.UtcNow;
            _ = await _userSessionRepository.UpdateAsync(session);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "刷新会话活跃时间失败，ConnectionId={ConnectionId}", ConnectionId);
        }
    }
}
