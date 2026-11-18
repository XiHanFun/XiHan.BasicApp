// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Operations.Auth;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 登录完成：开会话并签发令牌对
/// </summary>
/// <remarks>
/// 作用域服务。密码登录与两步验证的最后一步共用——清零失败计数与锁定、记最近登录、开会话、取角色并签发令牌对
/// 会话记下客户端地址与 User-Agent；登录日志补上会话 ID。开会话之前先过登录守卫（<see cref="ILoginGuard"/>，Access 按登录时机的访问策略判定），被拒绝时什么都不写
/// 用密码登录时按系统参数 <see cref="PasswordSettings.ConfigKey"/>：密码由他人设置且打开了强制改密，或密码超过有效期，会话一开即锁定到改密为止并提醒本人（验证码、通行密钥与第三方登录不走这一步）
/// 会话记下客户端报的设备标识。账号别的设备上还登录着时只提醒、不踢：给那些会话所在的侧各发一条安全通知，这台设备以前登录过即「其他设备登录」、否则「新设备登录」；别处都没登录着即在这一侧记一条「登录成功」
/// 用户的登录策略（不允许多端、限设备数）挤掉的会话也算别的设备：先收到这条提醒，再被吊销并强制下线
/// 提醒在会失败的读取之后、第一次写入之前发出，登录提醒排在改密提醒之前：跨侧的投递另起一次那一侧的调用、独立提交，前面有本次调用的写入时 SQLite 上会等自己的写锁
/// </remarks>
/// <param name="accounts">账号域服务</param>
/// <param name="users">用户仓储</param>
/// <param name="securities">账号安全仓储</param>
/// <param name="sessions">会话仓储</param>
/// <param name="issuer">令牌签发器</param>
/// <param name="guards">登录守卫</param>
/// <param name="activators">会话角色激活（令牌只带激活的角色）</param>
/// <param name="options">身份配置</param>
/// <param name="ids">标识生成器</param>
/// <param name="clock">时间源</param>
/// <param name="notices">发给本人的安全通知（登录提醒、改密提醒）</param>
public sealed class LoginSessions(
    UserAccountService accounts,
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSecurity, Guid> securities,
    IRepository<SysUserSession, Guid> sessions,
    AccessTokenIssuer issuer,
    IEnumerable<ILoginGuard> guards,
    IEnumerable<ISessionRoleActivator> activators,
    IdentityOptions options,
    IIdGenerator<Guid> ids,
    TimeProvider clock,
    SecurityNotices notices)
{
    /// <summary>
    /// 为已通过全部验证的账号开会话
    /// </summary>
    /// <param name="user">账号</param>
    /// <param name="security">账号安全（本方法写回清零的计数与锁定）</param>
    /// <param name="tenantId">登录的上下文；平台为 <see langword="null"/></param>
    /// <param name="tenantName">上下文的显示名</param>
    /// <param name="deviceId">客户端报上来的设备标识；没报为 <see langword="null"/></param>
    /// <param name="log">本次调用登记的登录日志</param>
    /// <param name="context">调用上下文</param>
    /// <param name="byPassword">这次登录是不是凭密码（验证码、第三方登录不是）</param>
    public async Task<Result<TokenResponse>> StartAsync(SysUser user, SysUserSecurity security, string? tenantId, string? tenantName, string? deviceId, LoginLogEntry log, InvocationContext context, bool byPassword = false)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(security);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(context);
        var roles = await accounts.RolesOfAsync(user.Id, tenantId, context.CancellationToken).ConfigureAwait(false);
        var guarded = await guards.CheckAllAsync(new LoginAttempt(tenantId ?? TenantIds.Platform, user.Id, user.UserName, roles), context).ConfigureAwait(false);
        if (guarded.IsFailure)
        {
            return guarded.Error!;
        }

        var now = clock.GetUtcNow();
        var mustChange = false;
        int? expiredAfterDays = null;
        if (byPassword)
        {
            var configured = await PasswordSettings.ReadAsync(context).ConfigureAwait(false);
            if (!configured.TryGetValue(out var settings))
            {
                return configured.Error!;
            }

            mustChange = security.MustChangePassword && settings.ForceChange;
            expiredAfterDays = !mustChange && settings.IsExpired(security.PasswordChangedAt, now) ? settings.ExpiryDays : null;
        }

        var locked = mustChange || expiredAfterDays.HasValue;
        var refreshToken = RefreshTokens.Generate();
        var session = new SysUserSession
        {
            Id = ids.NewId(),
            UserId = user.Id,
            ActiveTenantId = tenantId,
            RefreshTokenHash = RefreshTokens.Hash(refreshToken),
            IssuedAt = now,
            RefreshExpiresAt = now + options.RefreshTokenLifetime,
            IpAddress = SessionClient.AddressOf(context),
            ClientInfo = SessionClient.AgentOf(context),
            LockReason = locked ? SessionLockReasons.PasswordChangeRequired : null,
            LockedAt = locked ? now : null,
            SecurityStamp = security.SecurityStamp,
            DeviceId = deviceId,
        };
        SessionClient.Describe(session, context);
        var devices = await accounts.DevicesOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        var surplus = UserAccountService.SurplusOf(security, devices);
        var knownDevice = await accounts.IsKnownDeviceAsync(user.Id, deviceId, context.CancellationToken).ConfigureAwait(false);
        await notices.LoginAsync(user, session, knownDevice, devices, context).ConfigureAwait(false);
        if (mustChange)
        {
            await notices.PasswordChangeRequiredAsync(user, tenantId, context).ConfigureAwait(false);
        }
        else if (expiredAfterDays is { } days)
        {
            await notices.PasswordExpiredAsync(user, tenantId, days, context).ConfigureAwait(false);
        }

        security.FailedAccessCount = 0;
        security.LockoutEndAt = null;
        var securityUpdated = await securities.UpdateAsync(security, context.CancellationToken).ConfigureAwait(false);
        if (securityUpdated.IsFailure)
        {
            return securityUpdated.Error!;
        }

        user.LastLoginAt = now;
        user.LastLoginIp = SessionClient.AddressOf(context);
        var userUpdated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        if (userUpdated.IsFailure)
        {
            return userUpdated.Error!;
        }

        var added = await sessions.AddAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var enforced = await accounts.EnforceLoginPolicyAsync(surplus, context.CancellationToken).ConfigureAwait(false);
        if (enforced.IsFailure)
        {
            return enforced.Error!;
        }

        log.SessionId = session.Id;
        var active = await activators.StartAsync(session.Id, user.Id, session.ActiveTenantId, roles, context).ConfigureAwait(false);
        var access = issuer.Issue(user, session.Id, session.ActiveTenantId, active);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, session.RefreshExpiresAt, session.Id, session.ActiveTenantId, locked, UserResponse.From(user), tenantName);
    }
}
