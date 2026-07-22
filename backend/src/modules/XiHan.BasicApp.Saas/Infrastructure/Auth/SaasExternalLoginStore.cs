// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Auth;

/// <summary>
/// SaaS 第三方登录存储实现，桥接框架 <see cref="IExternalLoginStore"/> 与领域实体 SysExternalLogin
/// </summary>
public sealed class SaasExternalLoginStore : IExternalLoginStore
{
    private readonly ISqlSugarClientResolver _clientResolver;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasExternalLoginStore(ISqlSugarClientResolver clientResolver, ICurrentTenant currentTenant)
    {
        _clientResolver = clientResolver ?? throw new ArgumentNullException(nameof(clientResolver));
        _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
    }

    /// <inheritdoc />
    public async Task<long?> FindUserIdAsync(string provider, string providerKey, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerKey))
        {
            return null;
        }

        var db = _clientResolver.GetCurrentClient();
        var effectiveTenantId = tenantId ?? _currentTenant.Id ?? 0;

        var userId = await db.Queryable<SysExternalLogin>()
            .Where(l => l.Provider == provider
                        && l.ProviderKey == providerKey
                        && l.TenantId == effectiveTenantId
                        && !l.IsDeleted)
            .Select(l => l.UserId)
            .FirstAsync(cancellationToken);

        return userId == 0 ? null : userId;
    }

    /// <inheritdoc />
    public async Task CreateAsync(long userId, ExternalLoginInfo info, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(info);

        var db = _clientResolver.GetCurrentClient();
        var effectiveTenantId = tenantId ?? _currentTenant.Id ?? 0;

        var record = new SysExternalLogin
        {
            UserId = userId,
            TenantId = effectiveTenantId,
            Provider = info.Provider,
            ProviderKey = info.ProviderKey,
            ProviderDisplayName = info.DisplayName,
            Email = info.Email,
            AvatarUrl = info.AvatarUrl,
            LastLoginTime = DateTimeOffset.UtcNow
        };

        await db.Insertable(record).ExecuteCommandAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(long userId, string provider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("提供商名称不能为空。", nameof(provider));
        }

        var db = _clientResolver.GetCurrentClient();

        // 软删除：设置 IsDeleted = true
        await db.Updateable<SysExternalLogin>()
            .SetColumns(l => l.IsDeleted == true)
            .Where(l => l.UserId == userId && l.Provider == provider && !l.IsDeleted)
            .ExecuteCommandAsync(cancellationToken);
    }
}
