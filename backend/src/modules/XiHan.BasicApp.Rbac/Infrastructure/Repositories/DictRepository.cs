#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictRepository
// Guid:a5375509-02c2-43e3-9ee2-dea8bc59d2c3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:54:29
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 字典仓储实现
/// </summary>
public class DictRepository : SqlSugarAggregateRepository<SysDict, long>, IDictRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public DictRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientProvider, currentTenant, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    /// <param name="dictCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysDict?> GetByDictCodeAsync(string dictCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dictCode);
        var query = CreateTenantQueryable()
            .Where(dict => dict.DictCode == dictCode);

        if (tenantId.HasValue)
        {
            query = query.Where(dict => dict.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(dict => dict.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }
}
