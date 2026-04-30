#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionAppService
// Guid:ce0f9906-480c-42d5-8c82-632fb986c34d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户会话命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户会话")]
public sealed class UserSessionAppService(
    IUserSessionRepository userSessionRepository,
    IUserRepository userRepository,
    ILocalEventBus localEventBus,
    ICurrentUser currentUser)
    : SaasApplicationService, IUserSessionAppService
{
    /// <summary>
    /// 用户会话仓储
    /// </summary>
    private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 本地事件总线
    /// </summary>
    private readonly ILocalEventBus _localEventBus = localEventBus;

    /// <summary>
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 撤销用户会话
    /// </summary>
    /// <param name="input">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Revoke)]
    public async Task<UserSessionDetailDto> RevokeUserSessionAsync(UserSessionRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRevokeInput(input.BasicId, input.Reason, "会话主键必须大于 0。");

        var session = await GetSessionOrThrowAsync(input.BasicId, cancellationToken);
        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        if (!session.IsRevoked)
        {
            var reason = input.Reason.Trim();
            RevokeSession(session, reason, DateTimeOffset.UtcNow);
            session = await _userSessionRepository.UpdateAsync(session, cancellationToken);
            await PublishSessionRevokedAsync(session, revokeAllUserSessions: false, reason, cancellationToken);
        }

        return UserSessionApplicationMapper.ToDetailDto(session, user, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 撤销用户全部会话
    /// </summary>
    /// <param name="input">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>撤销会话数量</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Revoke)]
    public async Task<int> RevokeUserSessionsAsync(UserSessionsRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRevokeInput(input.UserId, input.Reason, "用户主键必须大于 0。");

        var user = await _userRepository.GetByIdAsync(input.UserId, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");
        var sessions = await _userSessionRepository.GetListAsync(
            session => session.UserId == user.BasicId && !session.IsRevoked,
            cancellationToken);

        if (sessions.Count == 0)
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        var reason = input.Reason.Trim();
        foreach (var session in sessions)
        {
            RevokeSession(session, reason, now);
        }

        _ = await _userSessionRepository.UpdateRangeAsync(sessions, cancellationToken);
        await PublishUserSessionsRevokedAsync(user, sessions[0].TenantId, reason, cancellationToken);
        return sessions.Count;
    }

    /// <summary>
    /// 获取会话，不存在时抛出异常
    /// </summary>
    private async Task<SysUserSession> GetSessionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        return await _userSessionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户会话不存在。");
    }

    /// <summary>
    /// 撤销会话
    /// </summary>
    private static void RevokeSession(SysUserSession session, string reason, DateTimeOffset now)
    {
        session.IsRevoked = true;
        session.RevokedAt = now;
        session.RevokedReason = reason;
        session.IsOnline = false;
        session.LogoutTime ??= now;
        session.LastActivityTime = now;
    }

    /// <summary>
    /// 发布单会话撤销事件
    /// </summary>
    private async Task PublishSessionRevokedAsync(SysUserSession session, bool revokeAllUserSessions, string reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _localEventBus.PublishAsync(
            new UserSessionRevokedDomainEvent(
                session.TenantId,
                session.UserId,
                session.BasicId,
                session.UserSessionId,
                session.CurrentAccessTokenJti,
                revokeAllUserSessions,
                _currentUser.UserId,
                reason));
    }

    /// <summary>
    /// 发布用户全部会话撤销事件
    /// </summary>
    private async Task PublishUserSessionsRevokedAsync(SysUser user, long sessionTenantKey, string reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _localEventBus.PublishAsync(
            new UserSessionRevokedDomainEvent(
                sessionTenantKey,
                user.BasicId,
                sessionId: null,
                userSessionId: null,
                accessTokenJti: null,
                revokeAllUserSessions: true,
                _currentUser.UserId,
                reason));
    }

    /// <summary>
    /// 校验撤销参数
    /// </summary>
    private static void ValidateRevokeInput(long id, string reason, string idMessage)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), idMessage);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (reason.Trim().Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), "撤销原因不能超过 200 个字符。");
        }
    }
}
