// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Security;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Infrastructure;

/// <summary>
/// <see cref="IApiCredentialDirectory"/> 的实现：签名开放接口在 Authenticate 阶段按 AppKey 取凭证（那时还没有工作单元，经 <see cref="StoreScope"/> 在独立作用域里只读）。
/// 不缓存：停用、删除、滚动密钥与改白名单都立即生效。账号不正常（停用、锁定中）时凭证照样取出，由调用方按 <see cref="ApiCredentialRecord.UserActive"/> 拒绝。
/// </summary>
/// <param name="scopes">作用域工厂。</param>
/// <param name="secrets">密钥保护。</param>
/// <param name="clock">时间源。</param>
public sealed class ApiCredentialDirectory(IServiceScopeFactory scopes, ISecretProtector secrets, TimeProvider clock) : IApiCredentialDirectory
{
    /// <summary>
    /// 最近使用时间的写入间隔：本实例一分钟内重复的调用不再写库。
    /// </summary>
    private static readonly TimeSpan _touchInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// 本实例上次写库的时间（按凭证）。
    /// </summary>
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _touched = new();

    /// <inheritdoc />
    public async ValueTask<ApiCredentialRecord?> FindAsync(string appKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(appKey) || appKey.Length > 64)
        {
            return null;
        }

        var now = clock.GetUtcNow();
        return await StoreScope.ReadAsync(scopes, async (services, ct) =>
        {
            var credentials = services.GetRequiredService<IRepository<SysUserApiCredential, Guid>>();
            var rows = await credentials.ListAsync(EntityQuery.All<SysUserApiCredential>().Where(new Specification<SysUserApiCredential>(c => c.AppKey == appKey)).Page(0, 1), ct).ConfigureAwait(false);
            if (rows.Count == 0 || !secrets.TryUnprotect(ApiCredentialService.SecretPurpose, rows[0].SecretCipher, out var secret))
            {
                return null;
            }

            var credential = rows[0];
            var user = await services.GetRequiredService<IRepository<SysUser, Guid>>().FindAsync(credential.UserId, ct).ConfigureAwait(false);
            if (!user.TryGetValue(out var owner))
            {
                return null;
            }

            var security = await services.GetRequiredService<UserAccountService>().SecurityOfAsync(owner.Id, ct).ConfigureAwait(false);
            if (!security.TryGetValue(out var account))
            {
                return null;
            }

            var active = owner.Status == UserStatus.Active && (account.LockoutEndAt is not { } lockout || lockout <= now);
            return new ApiCredentialRecord(
                credential.Id,
                credential.AppKey,
                credential.Name,
                secret,
                credential.IsEnabled,
                owner.Id,
                owner.UserName,
                owner.HomeTenantId,
                active,
                ApiCredentialService.AlgorithmOf(account),
                ApiCredentialService.AllowListOf(account));
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask TouchAsync(Guid credentialId, DateTimeOffset usedAt, CancellationToken cancellationToken)
    {
        if (_touched.TryGetValue(credentialId, out var last) && usedAt - last < _touchInterval)
        {
            return;
        }

        _touched[credentialId] = usedAt;
        await StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
            await unitOfWork.ConnectionFor(typeof(SysUserApiCredential)).Updateable<SysUserApiCredential>()
                .SetColumns(c => new SysUserApiCredential { LastUsedAt = usedAt })
                .Where(c => c.Id == credentialId)
                .ExecuteCommandAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }, cancellationToken).ConfigureAwait(false);
    }
}
