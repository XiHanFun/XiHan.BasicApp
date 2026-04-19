#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacTenantStore
// Guid:ce8064e6-5ed1-4527-a46a-d40ec35f726d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 17:28:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Core.Data;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;
using XiHan.Framework.MultiTenancy;
using XiHan.Framework.MultiTenancy.ConfigurationStore;

namespace XiHan.BasicApp.Saas.Infrastructure.MultiTenancy;

/// <summary>
/// 基于 RBAC 租户表的租户存储实现
/// </summary>
public class RbacTenantStore : ITenantStore
{
    private readonly ISqlSugarClientResolver _clientResolver;

    private ISqlSugarClient DbClient => _clientResolver.GetCurrentClient();

    /// <summary>
    /// 构造函数
    /// </summary>
    public RbacTenantStore(ISqlSugarClientResolver clientResolver)
    {
        _clientResolver = clientResolver;
    }

    /// <summary>
    /// 根据租户 Id 查询租户配置
    /// </summary>
    /// <param name="id">租户 Id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户配置</returns>
    public async Task<TenantConfiguration?> FindAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        var tenant = await CreateQuery()
            .Where(item => item.BasicId == id)
            .FirstAsync(cancellationToken);
        return tenant is null ? null : MapToConfiguration(tenant);
    }

    /// <summary>
    /// 根据租户名称查询租户配置
    /// </summary>
    /// <param name="name">租户名称（支持租户编码/租户名称）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户配置</returns>
    public async Task<TenantConfiguration?> FindAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        if (long.TryParse(trimmed, out var tenantId))
        {
            return await FindAsync(tenantId, cancellationToken);
        }

        var tenants = await CreateQuery().ToListAsync(cancellationToken);
        var tenant = tenants.FirstOrDefault(item =>
            string.Equals(item.TenantCode, trimmed, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.TenantName, trimmed, StringComparison.OrdinalIgnoreCase));
        return tenant is null ? null : MapToConfiguration(tenant);
    }

    /// <summary>
    /// 获取租户配置列表
    /// </summary>
    /// <param name="includeInactive">是否包含非激活租户</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户配置列表</returns>
    public async Task<IReadOnlyList<TenantConfiguration>> GetListAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var tenants = await CreateQuery()
            .OrderBy(item => item.Sort)
            .ToListAsync(cancellationToken);

        var configurations = tenants
            .Select(MapToConfiguration)
            .Where(item => includeInactive || item.IsActive)
            .ToList();

        return configurations;
    }

    private ISugarQueryable<SysTenant> CreateQuery()
    {
        // 租户存储需始终查询主租户视图，避免受当前租户过滤影响。
        return DbClient.Queryable<SysTenant>()
            .ClearFilter<IMultiTenantEntity>();
    }

    private static TenantConfiguration MapToConfiguration(SysTenant tenant)
    {
        var tenantCode = string.IsNullOrWhiteSpace(tenant.TenantCode)
            ? tenant.BasicId.ToString()
            : tenant.TenantCode.Trim();
        var normalizedName = tenantCode.ToUpperInvariant();

        var configuration = new TenantConfiguration(tenant.BasicId, tenantCode, normalizedName)
        {
            IsActive = IsActive(tenant)
        };

        if (!string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            configuration.ConnectionStrings = new ConnectionStrings
            {
                [ConnectionStrings.DefaultConnectionStringName] = tenant.ConnectionString.Trim()
            };
        }

        return configuration;
    }

    private static bool IsActive(SysTenant tenant)
    {
        return tenant.Status == YesOrNo.Yes &&
               tenant.TenantStatus == TenantStatus.Normal &&
               (!tenant.ExpireTime.HasValue || tenant.ExpireTime.Value > DateTimeOffset.UtcNow);
    }
}
