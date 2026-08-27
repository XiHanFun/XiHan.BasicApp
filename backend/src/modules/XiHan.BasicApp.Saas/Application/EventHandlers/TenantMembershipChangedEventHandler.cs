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
/// 租户成员变更事件处理器
/// </summary>
/// <remarks>
/// 当租户成员身份被撤销或过期时，移除该用户在该租户下的所有角色绑定，并失效该用户的授权快照缓存。
/// </remarks>
public sealed class TenantMembershipChangedEventHandler : ILocalEventHandler<TenantMembershipChangedDomainEvent>
{
    private readonly ISaasCacheInvalidator _cacheInvalidator;
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<TenantMembershipChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantMembershipChangedEventHandler(
        ISqlSugarClientResolver clientResolver,
        ISaasCacheInvalidator cacheInvalidator,
        ILogger<TenantMembershipChangedEventHandler> logger)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 处理租户成员变更事件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public async Task HandleEventAsync(TenantMembershipChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        _logger.LogInformation(
            "[TenantMembershipChanged] Membership changed for user {UserId} in tenant {TenantId}, status: {InviteStatus}, reason: {Reason}",
            eventData.UserId, eventData.TenantId, eventData.InviteStatus, eventData.Reason);

        // 当成员身份被撤销或过期时，移除该用户在此租户下的所有角色绑定
        if (eventData.InviteStatus is TenantMemberInviteStatus.Revoked or TenantMemberInviteStatus.Expired)
        {
            await RemoveUserRoleBindingsAsync(eventData.TenantId, eventData.UserId, eventData.InviteStatus, eventData.Reason);
        }
    }

    /// <summary>
    /// 移除用户在指定租户下的所有角色绑定
    /// </summary>
    private async Task RemoveUserRoleBindingsAsync(long tenantId, long userId, TenantMemberInviteStatus status, string? reason)
    {
        try
        {
            var db = _clientResolver.GetCurrentClient();

            // 查找该用户在此租户下的所有角色绑定
            var bindings = await db.Queryable<SysUserRole>()
                .Where(r => r.UserId == userId && r.TenantId == tenantId)
                .ToListAsync();

            if (bindings.Count == 0)
            {
                _logger.LogInformation(
                    "[TenantMembershipChanged] No role bindings found for user {UserId} in tenant {TenantId}",
                    userId, tenantId);
                return;
            }

            // 硬删除角色绑定（SysUserRole 支持硬删）
            var roleIds = bindings.Select(b => b.RoleId).ToList();
            await db.Deleteable<SysUserRole>()
                .Where(r => r.UserId == userId && r.TenantId == tenantId)
                .ExecuteCommandAsync();

            // 删了绑定不清授权快照 = 权限照旧生效：鉴权决策读的是缓存里的快照，
            // 与其它授权写路径（RoleAppService / UserRoleAppService 等）同一口径，按用户精准失效。
            await InvalidateAuthorizationCacheAsync(userId);

            _logger.LogWarning(
                "[TenantMembershipChanged] Removed {Count} role bindings for user {UserId} in tenant {TenantId}, status: {Status}, roles: [{RoleIds}]",
                bindings.Count, userId, tenantId, status, string.Join(", ", roleIds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[TenantMembershipChanged] Failed to remove role bindings for user {UserId} in tenant {TenantId}",
                userId, tenantId);
        }
    }

    /// <summary>
    /// 失效指定用户的授权快照缓存（该模式命中 user:{userId}:tenant:* ，覆盖该用户全部租户上下文）
    /// </summary>
    /// <remarks>
    /// 失效失败只记日志，不把已经落库的解绑动作回滚掉；快照另有 TTL 兜底。
    /// </remarks>
    private async Task InvalidateAuthorizationCacheAsync(long userId)
    {
        try
        {
            await _cacheInvalidator.InvalidateAuthorizationAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[TenantMembershipChanged] 用户 {UserId} 的授权快照缓存失效失败，被移除的角色权限最长要等缓存过期才失效", userId);
        }
    }
}
