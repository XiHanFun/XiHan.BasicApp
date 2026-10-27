// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// 密码登录（匿名操作，登记在架构测试的匿名白名单）：按注册地租户与登录名找账号，校验状态、锁定期与密码；成功即开一个会话并签发访问令牌与刷新令牌。
/// 失败计数在独立作用域里提交（<see cref="LoginAttempts"/>），否则随本次失败调用一起回滚。用户名或密码错误不区分哪一个。
/// </summary>
/// <param name="accounts">账号域服务。</param>
/// <param name="users">用户仓储。</param>
/// <param name="securities">账号安全仓储。</param>
/// <param name="sessions">会话仓储。</param>
/// <param name="attempts">失败计数。</param>
/// <param name="issuer">令牌签发器。</param>
/// <param name="options">身份配置。</param>
/// <param name="ids">标识生成器。</param>
/// <param name="clock">时间源。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http)]
public sealed class Login(
    UserAccountService accounts,
    IRepository<SysUser, Guid> users,
    IRepository<SysUserSecurity, Guid> securities,
    IRepository<SysUserSession, Guid> sessions,
    LoginAttempts attempts,
    AccessTokenIssuer issuer,
    IdentityOptions options,
    IIdGenerator<Guid> ids,
    TimeProvider clock) : IOperation<LoginRequest, TokenResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "auth.login";

    /// <inheritdoc />
    public async ValueTask<Result<TokenResponse>> HandleAsync(LoginRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var tenant = string.IsNullOrWhiteSpace(request.Tenant) ? TenantIds.Platform : request.Tenant.Trim();
        var found = await accounts.FindByNameAsync(tenant, request.UserName.Trim().ToLowerInvariant(), context.CancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var user))
        {
            return Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
        }

        if (user.Status != UserStatus.Active)
        {
            return Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.AccountUnavailable, MessageArguments.Of(("status", user.Status.ToString())));
        }

        var securityResult = await accounts.SecurityOfAsync(user.Id, context.CancellationToken).ConfigureAwait(false);
        if (!securityResult.TryGetValue(out var security))
        {
            return securityResult.Error!;
        }

        var now = clock.GetUtcNow();
        if (security.LockoutEndAt is { } lockoutEnd && lockoutEnd > now)
        {
            return LockedOut(lockoutEnd - now);
        }

        if (!PasswordHasher.Verify(request.Password, security.PasswordHash))
        {
            // 调用以失败结束时工作单元会回滚，计数在独立作用域里提交
            var lockedOut = await attempts.RecordFailureAsync(security.Id, context.CancellationToken).ConfigureAwait(false);
            return lockedOut ? LockedOut(options.LockoutDuration) : Error.Denied(IdentityErrors.InvalidCredentials, IdentityMessages.InvalidCredentials);
        }

        security.FailedAccessCount = 0;
        security.LockoutEndAt = null;
        var securityUpdated = await securities.UpdateAsync(security, context.CancellationToken).ConfigureAwait(false);
        if (securityUpdated.IsFailure)
        {
            return securityUpdated.Error!;
        }

        user.LastLoginAt = now;
        var userUpdated = await users.UpdateAsync(user, context.CancellationToken).ConfigureAwait(false);
        if (userUpdated.IsFailure)
        {
            return userUpdated.Error!;
        }

        var refreshToken = RefreshTokens.Generate();
        var session = new SysUserSession
        {
            Id = ids.NewId(),
            UserId = user.Id,
            ActiveTenantId = string.Equals(tenant, TenantIds.Platform, StringComparison.Ordinal) ? null : tenant,
            RefreshTokenHash = RefreshTokens.Hash(refreshToken),
            IssuedAt = now,
            RefreshExpiresAt = now + options.RefreshTokenLifetime,
        };
        var added = await sessions.AddAsync(session, context.CancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added.Error!;
        }

        var access = issuer.Issue(user, session.Id, session.ActiveTenantId, []);
        return new TokenResponse(access.Token, access.ExpiresAt, refreshToken, session.RefreshExpiresAt, session.Id, session.ActiveTenantId, security.MustChangePassword, UserResponse.From(user));
    }

    private static Error LockedOut(TimeSpan remaining)
        => Error.Denied(IdentityErrors.AccountUnavailable, IdentityMessages.LockedOut, MessageArguments.Of(("seconds", (int)Math.Ceiling(remaining.TotalSeconds))));
}
