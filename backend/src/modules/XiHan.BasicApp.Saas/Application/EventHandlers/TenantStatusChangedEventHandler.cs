#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantStatusChangedEventHandler
// Guid:2b3c4d5e-6f7a-8901-bcde-f12345678901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 租户状态变更事件处理器
/// </summary>
/// <remarks>
/// 当租户被暂停/过期/禁用时，撤销该租户下所有用户会话。
/// </remarks>
public sealed class TenantStatusChangedEventHandler : ILocalEventHandler<TenantStatusChangedDomainEvent>
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<TenantStatusChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantStatusChangedEventHandler(
        ISqlSugarClientResolver clientResolver,
        ILogger<TenantStatusChangedEventHandler> logger)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理租户状态变更事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(TenantStatusChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[TenantStatusChanged] Tenant {AffectedTenantId} status changed from {OldStatus} to {NewStatus}, operator: {OperatorUserId}, reason: {Reason}",
            eventData.AffectedTenantId, eventData.OldStatus, eventData.NewStatus,
            eventData.OperatorUserId, eventData.Reason);

        // 如果租户被暂停、过期或禁用，撤销该租户下所有用户会话
        if (eventData.NewStatus is TenantStatus.Suspended or TenantStatus.Expired or TenantStatus.Disabled)
        {
            await RevokeAllTenantSessionsAsync(eventData.AffectedTenantId, eventData.Reason);
        }
    }

    /// <summary>
    /// 撤销指定租户下所有活跃会话
    /// </summary>
    private async Task RevokeAllTenantSessionsAsync(long tenantId, string? reason)
    {
        var db = _clientResolver.GetCurrentClient();

        var activeSessions = await db.Queryable<SysUserSession>()
            .Where(s => s.TenantId == tenantId && s.Status == SessionStatus.Active && !s.IsDeleted)
            .ToListAsync();

        if (activeSessions.Count == 0)
        {
            _logger.LogInformation(
                "[TenantStatusChanged] No active sessions found for tenant {TenantId}", tenantId);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var session in activeSessions)
        {
            session.Status = SessionStatus.Revoked;
            session.RevokedAt = now;
            session.RevokedReason = reason ?? "Tenant status changed";
        }

        await db.Updateable(activeSessions)
            .UpdateColumns(s => new { s.Status, s.RevokedAt, s.RevokedReason })
            .ExecuteCommandAsync();

        _logger.LogWarning(
            "[TenantStatusChanged] Revoked {Count} active sessions for tenant {TenantId}, reason: {Reason}",
            activeSessions.Count, tenantId, reason);
    }
}
