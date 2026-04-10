#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppRepository
// Guid:9a82aca3-7a6b-47bb-b6cf-e6db0dcaf3bf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Data.SqlSugar.SplitTables;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// OAuth应用仓储实现
/// </summary>
public class OAuthAppRepository : SqlSugarAggregateRepository<SysOAuthApp, long>, IOAuthAppRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public OAuthAppRepository(
        ISqlSugarDbContext dbContext,
        ISqlSugarSplitTableExecutor splitTableExecutor,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, splitTableExecutor, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据客户端ID获取应用
    /// </summary>
    public async Task<SysOAuthApp?> GetByClientIdAsync(string clientId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        var resolvedTenantId = tenantId;

        var query = CreateTenantQueryable()
            .Where(app => app.ClientId == clientId);

        query = resolvedTenantId.HasValue
            ? query.Where(app => app.TenantId == resolvedTenantId.Value)
            : query.Where(app => app.TenantId == null);

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 校验客户端ID是否已存在
    /// </summary>
    public async Task<bool> IsClientIdExistsAsync(string clientId, long? tenantId = null, long? excludeAppId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        var resolvedTenantId = tenantId;

        var query = CreateTenantQueryable()
            .Where(app => app.ClientId == clientId);

        query = resolvedTenantId.HasValue
            ? query.Where(app => app.TenantId == resolvedTenantId.Value)
            : query.Where(app => app.TenantId == null);

        if (excludeAppId.HasValue)
        {
            query = query.Where(app => app.BasicId != excludeAppId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
