// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Infrastructure;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 账号域服务（作用域，用调用方的工作单元）：按名找用户、取安全行、设密码（策略 + 历史 + 安全戳）、吊销会话。业务失败返回 <see cref="Result"/>。
/// </summary>
public sealed class UserAccountService
{
    private readonly IRepository<SysUser, Guid> _users;
    private readonly IRepository<SysUserSecurity, Guid> _securities;
    private readonly IRepository<SysUserSession, Guid> _sessions;
    private readonly IRepository<SysPasswordHistory, Guid> _histories;
    private readonly IIdGenerator<Guid> _ids;
    private readonly IdentityOptions _options;
    private readonly SessionGate _gate;
    private readonly TimeProvider _clock;

    /// <summary>
    /// 构造服务。
    /// </summary>
    public UserAccountService(
        IRepository<SysUser, Guid> users,
        IRepository<SysUserSecurity, Guid> securities,
        IRepository<SysUserSession, Guid> sessions,
        IRepository<SysPasswordHistory, Guid> histories,
        IIdGenerator<Guid> ids,
        IdentityOptions options,
        SessionGate gate,
        TimeProvider clock)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _securities = securities ?? throw new ArgumentNullException(nameof(securities));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _histories = histories ?? throw new ArgumentNullException(nameof(histories));
        _ids = ids ?? throw new ArgumentNullException(nameof(ids));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// 按注册地租户与登录名找用户（不含已删除）。
    /// </summary>
    public async Task<Maybe<SysUser>> FindByNameAsync(string homeTenantId, string userName, CancellationToken cancellationToken)
    {
        var rows = await _users.ListAsync(EntityQuery.All<SysUser>().Where(new Specification<SysUser>(u => u.HomeTenantId == homeTenantId && u.UserName == userName)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? Maybe.None<SysUser>() : Maybe.Some(rows[0]);
    }

    /// <summary>
    /// 用户的安全行；没有即建一行空的（没有密码、不能登录）。
    /// </summary>
    public async Task<Result<SysUserSecurity>> SecurityOfAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await _securities.ListAsync(EntityQuery.All<SysUserSecurity>().Where(new Specification<SysUserSecurity>(s => s.UserId == userId)).Page(0, 1), cancellationToken).ConfigureAwait(false);
        if (rows.Count > 0)
        {
            return rows[0];
        }

        var security = new SysUserSecurity { Id = _ids.NewId(), UserId = userId, SecurityStamp = NewStamp() };
        var added = await _securities.AddAsync(security, cancellationToken).ConfigureAwait(false);
        return added.IsFailure ? added.Error! : security;
    }

    /// <summary>
    /// 设置密码：策略校验、与最近 N 个历史密码比对、写散列与安全戳、追加历史；<paramref name="mustChange"/> 为真时要求下次登录改密。
    /// </summary>
    public async Task<Result> SetPasswordAsync(SysUserSecurity security, string password, bool mustChange, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(security);
        var policy = PasswordPolicy.Validate(password, _options);
        if (policy.IsFailure)
        {
            return policy;
        }

        if (_options.PasswordHistoryDepth > 0)
        {
            var recent = await _histories.ListAsync(
                EntityQuery.All<SysPasswordHistory>().Where(new Specification<SysPasswordHistory>(h => h.UserId == security.UserId)).OrderBy(static h => h.CreatedAt, descending: true).Page(0, _options.PasswordHistoryDepth),
                cancellationToken).ConfigureAwait(false);
            if (recent.Any(h => PasswordHasher.Verify(password, h.PasswordHash)) || PasswordHasher.Verify(password, security.PasswordHash))
            {
                return Error.Validation(IdentityErrors.PasswordReused, IdentityMessages.PasswordReused, MessageArguments.Of(("depth", _options.PasswordHistoryDepth)));
            }
        }

        security.PasswordHash = PasswordHasher.Hash(password);
        security.PasswordChangedAt = _clock.GetUtcNow();
        security.SecurityStamp = NewStamp();
        security.MustChangePassword = mustChange;
        security.FailedAccessCount = 0;
        security.LockoutEndAt = null;
        var updated = await _securities.UpdateAsync(security, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        return await _histories.AddAsync(new SysPasswordHistory { Id = _ids.NewId(), UserId = security.UserId, PasswordHash = security.PasswordHash }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 吊销用户的有效会话（改密、重置、锁定时），并让门禁缓存失效；<paramref name="except"/> 指定要保留的会话（本人改密时保留当前会话）。
    /// </summary>
    public async Task<Result> RevokeSessionsAsync(Guid userId, string reason, CancellationToken cancellationToken, Guid? except = null)
    {
        var now = _clock.GetUtcNow();
        // 可空值类型的判空写 HasValue：SqlSugar 会把 `s.RevokedAt == null` 译成永假的 `revoked_at = null`；框架 bb663929（ADR-0035）起适配层已把 == null 改写成 HasValue，这里按改写后的形态直接写
        var active = await _sessions.ListAsync(EntityQuery.All<SysUserSession>().Where(new Specification<SysUserSession>(s => s.UserId == userId && !s.RevokedAt.HasValue)), cancellationToken).ConfigureAwait(false);
        if (except is { } keep)
        {
            active = active.Where(s => s.Id != keep).ToList();
        }

        if (active.Count == 0)
        {
            return Result.Success();
        }

        foreach (var session in active)
        {
            session.RevokedAt = now;
            session.RevokeReason = reason;
        }

        foreach (var outcome in await _sessions.UpdateRangeAsync(active, cancellationToken).ConfigureAwait(false))
        {
            if (outcome.IsFailure)
            {
                return outcome;
            }
        }

        foreach (var session in active)
        {
            await _gate.InvalidateAsync(session.Id, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <summary>
    /// 新安全戳。
    /// </summary>
    public static string NewStamp() => Guid.NewGuid().ToString("N");
}
