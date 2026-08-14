// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 部门仓储实现
/// </summary>
public sealed class DepartmentRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysDepartment>(clientResolver, unitOfWorkManager), IDepartmentRepository
{
    /// <summary>
    /// 根据当前租户和部门编码获取部门
    /// </summary>
    public async Task<SysDepartment?> GetByCodeAsync(string departmentCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(department => department.DepartmentCode == departmentCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取当前租户下的子部门
    /// </summary>
    public async Task<IReadOnlyList<SysDepartment>> GetChildrenAsync(long? parentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(department => department.ParentId == parentId)
            .OrderBy(department => department.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 当前租户下是否存在子部门
    /// </summary>
    public async Task<bool> HasChildrenAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(department => department.ParentId == departmentId)
            .AnyAsync(cancellationToken);
    }
}
