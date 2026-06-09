#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentRepository
// Guid:f13fb203-8a37-4173-91e0-92674b0f19d6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// <inheritdoc />
    public async Task<SysDepartment?> GetByCodeAsync(string departmentCode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(department => department.DepartmentCode == departmentCode)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SysDepartment>> GetChildrenAsync(long? parentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(department => department.ParentId == parentId)
            .OrderBy(department => department.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasChildrenAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(department => department.ParentId == departmentId)
            .AnyAsync(cancellationToken);
    }
}
