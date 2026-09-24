// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Auth;

/// <summary>
/// SaaS 第三方登录存储实现，桥接框架 <see cref="IExternalLoginStore"/> 与领域实体 SysExternalLogin
/// </summary>
/// <remarks>
/// 绑定行属于绑定时所在的租户（平台就是 0 号租户）：按指定租户查找与写入时切入该租户作用域，
/// 解绑按用户跨租户取出后按主键写（绑定是用户自有行）。
/// </remarks>
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

    /// <summary>
    /// 根据提供商和提供商用户标识查找关联的内部用户ID
    /// </summary>
    /// <param name="provider">提供商名称</param>
    /// <param name="providerKey">提供商用户标识</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken"></param>
    /// <returns>内部用户ID，未绑定返回 null</returns>
    public async Task<long?> FindUserIdAsync(string provider, string providerKey, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerKey))
        {
            return null;
        }

        var effectiveTenantId = tenantId ?? _currentTenant.Id ?? 0;
        using var tenantScope = _currentTenant.Change(effectiveTenantId);
        var db = _clientResolver.GetClientForEntity<SysExternalLogin>();

        var userId = await db.Queryable<SysExternalLogin>()
            .Where(l => l.Provider == provider
                        && l.ProviderKey == providerKey
                        && l.TenantId == effectiveTenantId
                        && !l.IsDeleted)
            .Select(l => l.UserId)
            .FirstAsync(cancellationToken);

        return userId == 0 ? null : userId;
    }

    /// <summary>
    /// 创建第三方登录绑定记录
    /// </summary>
    /// <param name="userId">内部用户ID</param>
    /// <param name="info">第三方登录信息</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken"></param>
    public async Task CreateAsync(long userId, ExternalLoginInfo info, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(info);

        var effectiveTenantId = tenantId ?? _currentTenant.Id ?? 0;
        using var tenantScope = _currentTenant.Change(effectiveTenantId);
        var db = _clientResolver.GetClientForEntity<SysExternalLogin>();

        var record = new SysExternalLogin
        {
            UserId = userId,
            Provider = info.Provider,
            ProviderKey = info.ProviderKey,
            ProviderDisplayName = info.DisplayName,
            Email = info.Email,
            AvatarUrl = info.AvatarUrl,
            LastLoginTime = DateTimeOffset.UtcNow
        };

        await db.Insertable(record).ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 删除第三方登录绑定记录
    /// </summary>
    /// <param name="userId">内部用户ID</param>
    /// <param name="provider">提供商名称</param>
    /// <param name="cancellationToken"></param>
    public async Task RemoveAsync(long userId, string provider, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("提供商名称不能为空。", nameof(provider));
        }

        var db = _clientResolver.GetClientForEntity<SysExternalLogin>();

        // 绑定行带绑定时所在租户的戳：按用户跨租户取出，再按主键软删
        // （表达式式更新会被自动挂上当前作用域的租户过滤，别的租户戳的绑定就删不到）
        var bindings = await db.Queryable<SysExternalLogin>()
            .ClearTenantFilter()
            .Where(l => l.UserId == userId && l.Provider == provider && !l.IsDeleted)
            .ToListAsync(cancellationToken);
        if (bindings.Count == 0)
        {
            return;
        }

        foreach (var binding in bindings)
        {
            binding.IsDeleted = true;
        }

        await db.Updateable(bindings)
            .UpdateColumns(l => new { l.IsDeleted })
            .ExecuteCommandAsync(cancellationToken);
    }
}
