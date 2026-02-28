#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentRepository
// Guid:856687ef-ac26-4c4a-9191-bf1caa7171f1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:53:35
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
/// 部门仓储实现
/// </summary>
public class DepartmentRepository : SqlSugarAggregateRepository<SysDepartment, long>, IDepartmentRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="currentTenant"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="unitOfWorkManager"></param>
    public DepartmentRepository(
        ISqlSugarClientProvider clientProvider,
        ICurrentTenant currentTenant,
        IServiceProvider serviceProvider,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientProvider, currentTenant, serviceProvider, unitOfWorkManager)
    {
    }

    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    /// <param name="departmentCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);
        var query = CreateTenantQueryable()
            .Where(department => department.DepartmentCode == departmentCode);

        if (tenantId.HasValue)
        {
            query = query.Where(department => department.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(department => department.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取子部门
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysDepartment>> GetChildrenAsync(long? parentId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = CreateTenantQueryable()
            .Where(department => department.ParentId == parentId);

        if (tenantId.HasValue)
        {
            query = query.Where(department => department.TenantId == tenantId.Value);
        }

        return await query.OrderBy(department => department.Sort).ToListAsync(cancellationToken);
    }
}
