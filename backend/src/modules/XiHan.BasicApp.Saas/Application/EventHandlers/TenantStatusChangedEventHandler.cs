// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// 租户状态变更事件处理器
/// </summary>
/// <remarks>
/// 当租户被暂停/过期/禁用时，撤销该租户下所有用户会话，并失效会话状态缓存。
/// </remarks>
public sealed class TenantStatusChangedEventHandler : ILocalEventHandler<TenantStatusChangedDomainEvent>
{
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<TenantStatusChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantStatusChangedEventHandler(
        ISqlSugarClientResolver clientResolver,
        ISaasCacheInvalidator cacheInvalidator,
        ILogger<TenantStatusChangedEventHandler> logger)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
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

        // 除本租户的会话外，还要带上本租户用户借身份进别的租户的模仿会话：
        // 那些行的租户戳是目标租户，只有 ImpersonatorTenantId 才是本租户
        var activeSessions = await db.Queryable<SysUserSession>()
            .Where(s => (s.TenantId == tenantId || s.ImpersonatorTenantId == tenantId)
                && s.Status == SessionStatus.Active && !s.IsDeleted)
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
            session.RevokedTime = now;
            session.RevokedReason = reason ?? "Tenant status changed";
        }

        await db.Updateable(activeSessions)
            .UpdateColumns(s => new { s.Status, s.RevokedTime, s.RevokedReason })
            .ExecuteCommandAsync();

        // 只改库不清缓存 = 白改：会话闸门每请求读的是 60 秒 TTL 的会话状态缓存，
        // 被停租户的用户最长还能再通过闸门 60 秒。这一刀才是"停租户立刻生效"的关键。
        // 批量吊销按整体清空（一次调用），不逐会话清：租户停用是低频动作，
        // 逐条清要 N 次远程往返，而多清出去的其它会话只是回落数据库重建，无正确性风险。
        await InvalidateSessionStateCacheAsync(tenantId);

        _logger.LogWarning(
            "[TenantStatusChanged] Revoked {Count} active sessions for tenant {TenantId}, reason: {Reason}",
            activeSessions.Count, tenantId, reason);
    }

    /// <summary>
    /// 失效会话状态缓存（失败只记日志，不阻断吊销主流程——缓存有 60 秒短 TTL 兜底）
    /// </summary>
    private async Task InvalidateSessionStateCacheAsync(long tenantId)
    {
        try
        {
            await _cacheInvalidator.InvalidateAllSessionStatesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[TenantStatusChanged] 会话状态缓存失效失败，租户 {TenantId} 的吊销最长要等缓存过期才生效", tenantId);
        }
    }
}
