// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户部门仓储实现
/// </summary>
public sealed class UserDepartmentRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserDepartment>(clientResolver), IUserDepartmentRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysUserDepartment>> GetValidByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(department => department.UserId == userId)
            .Where(department => department.Status == ValidityStatus.Valid)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> GetUserIdsByDepartmentIdsAsync(IEnumerable<long> departmentIds, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ids = departmentIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        return await CreateQueryable()
            .Where(department => ids.Contains(department.DepartmentId))
            .Where(department => department.Status == ValidityStatus.Valid)
            .Select(department => department.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
