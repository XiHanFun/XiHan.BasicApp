// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Caching.Abstractions.Caches;
using XiHan.Framework.Caching.Abstractions.Stores;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 会话门禁（单例）：按访问令牌的 <c>sid</c> 核对会话仍有效（未吊销、未过期、用户正常；模仿会话还要求发起人的原会话仍有效）。
/// 在 Authenticate 阶段被调用，那时还没有工作单元，所以经 <see cref="StoreScope"/> 在独立作用域里只读；结果进共享缓存，存活 <see cref="IdentityOptions.SessionCacheTimeToLive"/>，吊销时主动失效。
/// </summary>
public sealed class SessionGate
{
    private const string Valid = "valid";
    private const string Invalid = "invalid";
    private const string LockedPrefix = "locked:";

    private readonly ISharedCache _cache;
    private readonly IServiceScopeFactory _scopes;
    private readonly IdentityOptions _options;
    private readonly TimeProvider _clock;

    /// <summary>
    /// 构造门禁。
    /// </summary>
    public SessionGate(ISharedCache cache, IServiceScopeFactory scopes, IdentityOptions options, TimeProvider clock)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// 会话是否仍可用；不可用返回 <c>IDENTITY_DENIED_001</c>，锁定返回 <c>IDENTITY_DENIED_002</c>（参数 <c>reason</c>）。
    /// </summary>
    public async ValueTask<Result> ValidateAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var state = await _cache.GetOrCreateAsync(KeyOf(sessionId), ct => LoadAsync(sessionId, ct), CacheEntryOptions.Expires(_options.SessionCacheTimeToLive), cancellationToken).ConfigureAwait(false);
        if (string.Equals(state, Valid, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        return state.StartsWith(LockedPrefix, StringComparison.Ordinal)
            ? Error.Denied(IdentityErrors.SessionLocked, IdentityMessages.SessionLocked, MessageArguments.Of(("reason", state[LockedPrefix.Length..])))
            : Error.Denied(IdentityErrors.SessionInvalid, IdentityMessages.SessionInvalid);
    }

    /// <summary>
    /// 吊销后让缓存立即失效（本实例立即生效，其他实例最迟在存活时间后生效）。
    /// </summary>
    public ValueTask<bool> InvalidateAsync(Guid sessionId, CancellationToken cancellationToken) => _cache.RemoveAsync(KeyOf(sessionId), cancellationToken);

    private static CacheKey KeyOf(Guid sessionId) => new($"identity:session:{sessionId:N}");

    private async ValueTask<string> LoadAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        return await StoreScope.ReadAsync(_scopes, async (services, ct) =>
        {
            var sessions = services.GetRequiredService<IRepository<SysUserSession, Guid>>();
            var session = await sessions.FindAsync(sessionId, ct).ConfigureAwait(false);
            if (!session.TryGetValue(out var row) || !row.IsActive(now))
            {
                return Invalid;
            }

            // 模仿会话随发起人的原会话一起失效：发起人注销或被吊销后，被模仿身份的令牌也不能再用
            if (row.ImpersonatorSessionId is { } originId)
            {
                var origin = await sessions.FindAsync(originId, ct).ConfigureAwait(false);
                if (!origin.TryGetValue(out var originRow) || !originRow.IsActive(now))
                {
                    return Invalid;
                }
            }

            var user = await services.GetRequiredService<IRepository<SysUser, Guid>>().FindAsync(row.UserId, ct).ConfigureAwait(false);
            if (!user.TryGetValue(out var account) || account.Status != UserStatus.Active)
            {
                return Invalid;
            }

            return row.LockReason is { } reason ? LockedPrefix + reason : Valid;
        }, cancellationToken).ConfigureAwait(false);
    }
}
