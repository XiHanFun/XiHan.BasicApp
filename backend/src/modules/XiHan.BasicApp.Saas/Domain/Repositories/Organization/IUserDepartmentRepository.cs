// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户部门仓储接口
/// </summary>
public interface IUserDepartmentRepository : ISaasRepository<SysUserDepartment>
{
    /// <summary>
    /// 获取用户有效部门归属
    /// </summary>
    Task<IReadOnlyList<SysUserDepartment>> GetValidByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取归属指定部门集合的有效用户主键集合（用于数据范围过滤）
    /// </summary>
    Task<IReadOnlyList<long>> GetUserIdsByDepartmentIdsAsync(IEnumerable<long> departmentIds, CancellationToken cancellationToken = default);
}
