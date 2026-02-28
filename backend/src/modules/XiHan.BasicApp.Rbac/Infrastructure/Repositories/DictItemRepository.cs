#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictItemRepository
// Guid:72021d33-0690-412b-bbc5-e10555bb509c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:56:39
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 字典项仓储实现
/// </summary>
public class DictItemRepository : SqlSugarRepositoryBase<SysDictItem, long>, IDictItemRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    public DictItemRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider)
        : base(clientProvider, currentTenant, serviceProvider)
    {
    }

    /// <summary>
    /// 根据字典ID获取字典项
    /// </summary>
    /// <param name="dictId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IReadOnlyList<SysDictItem>> GetByDictIdAsync(long dictId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId.HasValue)
        {
            return GetListAsync(item => item.DictId == dictId && item.TenantId == tenantId.Value, item => item.Sort, cancellationToken);
        }

        return GetListAsync(item => item.DictId == dictId, item => item.Sort, cancellationToken);
    }
}
