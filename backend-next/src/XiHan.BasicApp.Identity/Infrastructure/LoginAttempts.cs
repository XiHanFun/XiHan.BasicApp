// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// 登录失败计数。
/// </summary>
/// <remarks>
/// 单例服务。登录以失败结束时调用方的工作单元会整体回滚，计数与锁定必须在独立作用域里自己提交，否则永远锁不住。
/// </remarks>
public sealed class LoginAttempts
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IdentityOptions _options;
    private readonly TimeProvider _clock;

    /// <summary>
    /// 构造计数器。
    /// </summary>
    public LoginAttempts(IServiceScopeFactory scopes, IdentityOptions options, TimeProvider clock)
    {
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>
    /// 记一次失败。
    /// </summary>
    /// <remarks>
    /// 计数加一，达到上限即锁定并清零。
    /// </remarks>
    /// <returns>是否因本次失败进入锁定。</returns>
    public async Task<bool> RecordFailureAsync(Guid securityId, CancellationToken cancellationToken)
    {
        var lockedOut = false;
        var written = await StoreScope.WriteAsync(_scopes, async (services, ct) =>
        {
            var securities = services.GetRequiredService<IRepository<SysUserSecurity, Guid>>();
            if (!(await securities.FindAsync(securityId, ct).ConfigureAwait(false)).TryGetValue(out var security))
            {
                return Result.Success();
            }

            security.FailedAccessCount++;
            if (security.FailedAccessCount >= _options.MaxFailedAccessAttempts)
            {
                security.LockoutEndAt = _clock.GetUtcNow() + _options.LockoutDuration;
                security.FailedAccessCount = 0;
                lockedOut = true;
            }

            return await securities.UpdateAsync(security, ct).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
        written.ThrowIfFailure();
        return lockedOut;
    }
}
