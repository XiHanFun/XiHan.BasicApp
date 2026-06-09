#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMembershipChangedEventHandler
// Guid:6f7a8b9c-0d1e-2345-fabc-456789012345
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
/// 租户成员变更事件处理器
/// </summary>
/// <remarks>
/// 当租户成员身份被撤销或过期时，移除该用户在该租户下的所有角色绑定。
/// </remarks>
public sealed class TenantMembershipChangedEventHandler : ILocalEventHandler<TenantMembershipChangedDomainEvent>
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ILogger<TenantMembershipChangedEventHandler> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantMembershipChangedEventHandler(
        ISqlSugarClientResolver clientResolver,
        ILogger<TenantMembershipChangedEventHandler> logger)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
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
}
