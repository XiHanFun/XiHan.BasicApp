#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasDomainEventHandler
// Guid:2f4e1a8c-7a4a-47d3-8e9d-0c39df8b6a62
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.EventHandlers;

/// <summary>
/// SaaS 领域事件处理器
/// </summary>
[UnitOfWork(true)]
public sealed class SaasDomainEventHandler(
    IUserSessionRepository userSessionRepository,
    IOAuthTokenRepository oauthTokenRepository,
    ISessionRoleRepository sessionRoleRepository,
    ISqlSugarClientResolver clientResolver,
    ICurrentTenant currentTenant)
    : ILocalEventHandler<AuthorizationChangedDomainEvent>,
      ILocalEventHandler<DataScopeChangedDomainEvent>,
      ILocalEventHandler<FieldLevelSecurityChangedDomainEvent>,
      ILocalEventHandler<HierarchyChangedDomainEvent>,
      ILocalEventHandler<TenantMembershipChangedDomainEvent>,
      ILocalEventHandler<TenantStatusChangedDomainEvent>,
      ILocalEventHandler<UserSessionRevokedDomainEvent>,
      ITransientDependency
{
    private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository = oauthTokenRepository;
    private readonly ISessionRoleRepository _sessionRoleRepository = sessionRoleRepository;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    private ISqlSugarClient DbClient => clientResolver.GetCurrentClient();

    /// <inheritdoc />
    public async Task HandleEventAsync(AuthorizationChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await WritePermissionChangeLogAsync(eventData);
        await WriteDiffLogAsync(
            eventData,
            nameof(AuthorizationChangedDomainEvent),
            eventData.TargetId.ToString(),
            OperationType.Update,
            AuditRiskLevel.High,
            $"授权变更：{eventData.TargetType}#{eventData.TargetId} 权限 {eventData.PermissionId} {eventData.Action}",
            new
            {
                eventData.TargetType,
                eventData.TargetId,
                eventData.PermissionId,
                eventData.Action
            });
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(DataScopeChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await WriteDiffLogAsync(
            eventData,
            nameof(DataScopeChangedDomainEvent),
            eventData.TargetId.ToString(),
            OperationType.Update,
            AuditRiskLevel.High,
            $"数据范围变更：{eventData.TargetType}#{eventData.TargetId} => {eventData.DataScope}",
            new
            {
                eventData.TargetType,
                eventData.TargetId,
                eventData.DataScope
            });
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(FieldLevelSecurityChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await WriteDiffLogAsync(
            eventData,
            nameof(FieldLevelSecurityChangedDomainEvent),
            eventData.FieldSecurityId.ToString(),
            OperationType.Update,
            AuditRiskLevel.Critical,
            $"字段级安全变更：{eventData.TargetType}#{eventData.TargetId} {eventData.FieldName}",
            new
            {
                eventData.FieldSecurityId,
                eventData.TargetType,
                eventData.TargetId,
                eventData.ResourceId,
                eventData.FieldName,
                eventData.IsReadable,
                eventData.IsEditable,
                eventData.MaskStrategy
            });
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(HierarchyChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await WriteDiffLogAsync(
            eventData,
            nameof(HierarchyChangedDomainEvent),
            eventData.NodeId.ToString(),
            OperationType.Update,
            AuditRiskLevel.Medium,
            $"层级关系变更：{eventData.HierarchyType}#{eventData.NodeId} parent={eventData.ParentId?.ToString() ?? "null"}",
            new
            {
                eventData.HierarchyType,
                eventData.NodeId,
                eventData.ParentId
            });
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(TenantMembershipChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await WriteDiffLogAsync(
            eventData,
            nameof(TenantMembershipChangedDomainEvent),
            eventData.UserId.ToString(),
            OperationType.Update,
            AuditRiskLevel.High,
            $"租户成员变更：User#{eventData.UserId} => {eventData.InviteStatus}",
            new
            {
                eventData.UserId,
                eventData.InviteStatus
            });
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(TenantStatusChangedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.NewStatus != TenantStatus.Normal)
        {
            await RevokeTenantSessionsAsync(eventData);
        }

        await WriteDiffLogAsync(
            eventData,
            nameof(TenantStatusChangedDomainEvent),
            eventData.AffectedTenantId.ToString(),
            OperationType.Update,
            eventData.NewStatus == TenantStatus.Normal ? AuditRiskLevel.Medium : AuditRiskLevel.Critical,
            $"租户状态变更：Tenant#{eventData.AffectedTenantId} {eventData.OldStatus} => {eventData.NewStatus}",
            new
            {
                eventData.AffectedTenantId,
                eventData.OldStatus,
                eventData.NewStatus
            });
    }

    /// <inheritdoc />
    public async Task HandleEventAsync(UserSessionRevokedDomainEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        await RevokeUserSessionRelatedStateAsync(eventData);
        await WriteDiffLogAsync(
            eventData,
            nameof(UserSessionRevokedDomainEvent),
            eventData.SessionId?.ToString() ?? eventData.UserId.ToString(),
            OperationType.Update,
            AuditRiskLevel.High,
            eventData.RevokeAllUserSessions
                ? $"用户全部会话撤销：User#{eventData.UserId}"
                : $"用户会话撤销：Session#{eventData.SessionId?.ToString() ?? eventData.UserSessionId ?? "unknown"}",
            new
            {
                eventData.UserId,
                eventData.SessionId,
                eventData.UserSessionId,
                eventData.AccessTokenJti,
                eventData.RevokeAllUserSessions
            });
    }

    /// <summary>
    /// 租户进入非正常状态时撤销该租户内所有在线会话、Token 与会话角色。
    /// </summary>
    private async Task RevokeTenantSessionsAsync(TenantStatusChangedDomainEvent eventData)
    {
        using var tenantScope = _currentTenant.Change(eventData.AffectedTenantId, eventData.AffectedTenantId.ToString());
        var now = DateTimeOffset.UtcNow;
        var reason = NormalizeText(eventData.Reason, 200) ?? $"租户状态变更为 {eventData.NewStatus}";

        var sessions = await _userSessionRepository.GetListAsync(session => !session.IsRevoked);
        if (sessions.Count > 0)
        {
            foreach (var session in sessions)
            {
                RevokeSession(session, now, reason);
            }

            _ = await _userSessionRepository.UpdateRangeAsync(sessions);
        }

        await RevokeOAuthTokensByTenantAsync(now);
        await DeactivateSessionRolesByTenantAsync(now, reason);
    }

    /// <summary>
    /// 撤销会话事件关联的 Token 与会话角色。
    /// </summary>
    private async Task RevokeUserSessionRelatedStateAsync(UserSessionRevokedDomainEvent eventData)
    {
        var now = DateTimeOffset.UtcNow;
        var reason = NormalizeText(eventData.Reason, 200) ?? "会话撤销";

        if (eventData.RevokeAllUserSessions)
        {
            var sessions = await _userSessionRepository.GetListAsync(session => session.UserId == eventData.UserId);
            var activeSessions = sessions.Where(session => !session.IsRevoked).ToArray();
            foreach (var session in activeSessions)
            {
                RevokeSession(session, now, reason);
            }

            if (activeSessions.Length > 0)
            {
                _ = await _userSessionRepository.UpdateRangeAsync(activeSessions);
            }

            await RevokeOAuthTokensByUserAsync(eventData.UserId, now);
            await DeactivateSessionRolesAsync(sessions.Select(session => session.BasicId).ToArray(), now, reason);
            return;
        }

        var sessionIds = await ResolveSessionIdsAsync(eventData);
        if (sessionIds.Length > 0)
        {
            var sessions = await _userSessionRepository.GetListAsync(session => sessionIds.Contains(session.BasicId));
            var activeSessions = sessions.Where(session => !session.IsRevoked).ToArray();
            foreach (var session in activeSessions)
            {
                RevokeSession(session, now, reason);
            }

            if (activeSessions.Length > 0)
            {
                _ = await _userSessionRepository.UpdateRangeAsync(activeSessions);
            }

            await DeactivateSessionRolesAsync(sessionIds, now, reason);
        }

        await RevokeOAuthTokensBySessionAsync(sessionIds, eventData.AccessTokenJti, now);
    }

    /// <summary>
    /// 解析事件关联的会话主键。
    /// </summary>
    private async Task<long[]> ResolveSessionIdsAsync(UserSessionRevokedDomainEvent eventData)
    {
        if (eventData.SessionId.HasValue && eventData.SessionId.Value > 0)
        {
            return [eventData.SessionId.Value];
        }

        if (!string.IsNullOrWhiteSpace(eventData.UserSessionId))
        {
            var userSessionId = eventData.UserSessionId.Trim();
            var sessions = await _userSessionRepository.GetListAsync(session => session.UserSessionId == userSessionId);
            return [.. sessions.Select(session => session.BasicId).Distinct()];
        }

        if (!string.IsNullOrWhiteSpace(eventData.AccessTokenJti))
        {
            var accessTokenJti = eventData.AccessTokenJti.Trim();
            var sessions = await _userSessionRepository.GetListAsync(session => session.CurrentAccessTokenJti == accessTokenJti);
            return [.. sessions.Select(session => session.BasicId).Distinct()];
        }

        return [];
    }

    /// <summary>
    /// 撤销指定用户的全部 OAuth Token。
    /// </summary>
    private async Task RevokeOAuthTokensByUserAsync(long userId, DateTimeOffset now)
    {
        var tokens = await _oauthTokenRepository.GetListAsync(token => token.UserId == userId && !token.IsRevoked);
        await RevokeOAuthTokensAsync(tokens, now);
    }

    /// <summary>
    /// 撤销指定会话或 JTI 关联的 OAuth Token。
    /// </summary>
    private async Task RevokeOAuthTokensBySessionAsync(long[] sessionIds, string? accessTokenJti, DateTimeOffset now)
    {
        var hasSessionIds = sessionIds.Length > 0;
        var normalizedJti = NormalizeText(accessTokenJti, 200);

        if (!hasSessionIds && string.IsNullOrWhiteSpace(normalizedJti))
        {
            return;
        }

        var tokens = await _oauthTokenRepository.GetListAsync(token =>
            !token.IsRevoked &&
            ((hasSessionIds && token.SessionId.HasValue && sessionIds.Contains(token.SessionId.Value)) ||
             (!string.IsNullOrWhiteSpace(normalizedJti) && token.AccessTokenJti == normalizedJti)));

        await RevokeOAuthTokensAsync(tokens, now);
    }

    /// <summary>
    /// 撤销当前租户全部 OAuth Token。
    /// </summary>
    private async Task RevokeOAuthTokensByTenantAsync(DateTimeOffset now)
    {
        var tokens = await _oauthTokenRepository.GetListAsync(token => !token.IsRevoked);
        await RevokeOAuthTokensAsync(tokens, now);
    }

    /// <summary>
    /// 批量撤销 OAuth Token。
    /// </summary>
    private async Task RevokeOAuthTokensAsync(IReadOnlyCollection<SysOAuthToken> tokens, DateTimeOffset now)
    {
        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedTime = now;
        }

        _ = await _oauthTokenRepository.UpdateRangeAsync(tokens);
    }

    /// <summary>
    /// 停用指定会话下的激活角色。
    /// </summary>
    private async Task DeactivateSessionRolesAsync(long[] sessionIds, DateTimeOffset now, string reason)
    {
        if (sessionIds.Length == 0)
        {
            return;
        }

        var sessionRoles = await _sessionRoleRepository.GetListAsync(role =>
            sessionIds.Contains(role.SessionId) && role.Status == SessionRoleStatus.Active);

        await DeactivateSessionRolesAsync(sessionRoles, now, reason);
    }

    /// <summary>
    /// 停用当前租户内全部激活会话角色。
    /// </summary>
    private async Task DeactivateSessionRolesByTenantAsync(DateTimeOffset now, string reason)
    {
        var sessionRoles = await _sessionRoleRepository.GetListAsync(role => role.Status == SessionRoleStatus.Active);
        await DeactivateSessionRolesAsync(sessionRoles, now, reason);
    }

    /// <summary>
    /// 批量停用会话角色。
    /// </summary>
    private async Task DeactivateSessionRolesAsync(IReadOnlyCollection<SysSessionRole> sessionRoles, DateTimeOffset now, string reason)
    {
        if (sessionRoles.Count == 0)
        {
            return;
        }

        foreach (var sessionRole in sessionRoles)
        {
            sessionRole.Status = SessionRoleStatus.Inactive;
            sessionRole.DeactivatedAt = now;
            sessionRole.Reason = NormalizeText(reason, 500);
        }

        _ = await _sessionRoleRepository.UpdateRangeAsync(sessionRoles);
    }

    /// <summary>
    /// 标记会话为撤销。
    /// </summary>
    private static void RevokeSession(SysUserSession session, DateTimeOffset now, string reason)
    {
        session.IsRevoked = true;
        session.RevokedAt = now;
        session.RevokedReason = reason;
        session.IsOnline = false;
        session.LogoutTime ??= now;
        session.LastActivityTime = now;
    }

    /// <summary>
    /// 写入权限变更日志。
    /// </summary>
    private async Task WritePermissionChangeLogAsync(AuthorizationChangedDomainEvent eventData)
    {
        var now = DateTimeOffset.UtcNow;
        var isRoleTarget = IsTargetType(eventData.TargetType, "role");
        var entity = new SysPermissionChangeLog
        {
            TenantId = eventData.TenantId,
            OperatorUserId = eventData.OperatorUserId,
            TargetRoleId = isRoleTarget ? eventData.TargetId : null,
            TargetUserId = isRoleTarget ? null : eventData.TargetId,
            PermissionId = eventData.PermissionId,
            ChangeType = ResolvePermissionChangeType(isRoleTarget, eventData.Action),
            ChangeReason = NormalizeText(eventData.Reason, 500),
            Description = NormalizeText($"授权变更：{eventData.TargetType}#{eventData.TargetId} 权限 {eventData.PermissionId} {eventData.Action}", 500),
            ChangeTime = now,
            CreatedTime = now
        };

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }

    /// <summary>
    /// 写入通用领域事件差异日志。
    /// </summary>
    private async Task WriteDiffLogAsync(
        SaasDomainEventBase eventData,
        string entityType,
        string entityId,
        OperationType operationType,
        AuditRiskLevel riskLevel,
        string description,
        object extendData)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new SysDiffLog
        {
            TenantId = eventData.TenantId,
            UserId = eventData.OperatorUserId,
            AuditType = "DomainEvent",
            OperationType = operationType,
            EntityType = NormalizeText(entityType, 100),
            EntityId = NormalizeText(entityId, 100),
            EntityName = NormalizeText(entityType, 200),
            TableName = NormalizeText(entityType, 100),
            PrimaryKey = "BasicId",
            PrimaryKeyValue = NormalizeText(entityId, 100),
            Description = NormalizeText(description, 500),
            ChangeDescription = NormalizeText(description, 1000),
            IsSuccess = true,
            RiskLevel = riskLevel,
            AuditTime = now,
            ExtendData = JsonSerializer.Serialize(extendData),
            Remark = NormalizeText(eventData.Reason, 500),
            CreatedTime = now
        };

        await DbClient.Insertable(entity).SplitTable().ExecuteCommandAsync();
    }

    /// <summary>
    /// 解析权限变更类型。
    /// </summary>
    private static PermissionChangeType ResolvePermissionChangeType(bool isRoleTarget, PermissionAction action)
    {
        return (isRoleTarget, action) switch
        {
            (true, PermissionAction.Deny) => PermissionChangeType.RoleDenyPermission,
            (true, _) => PermissionChangeType.RoleGrantPermission,
            (false, PermissionAction.Deny) => PermissionChangeType.UserDenyPermission,
            (false, _) => PermissionChangeType.UserGrantPermission
        };
    }

    /// <summary>
    /// 判断目标类型。
    /// </summary>
    private static bool IsTargetType(string targetType, string keyword)
    {
        return targetType.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 规范化字符串长度。
    /// </summary>
    private static string? NormalizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
