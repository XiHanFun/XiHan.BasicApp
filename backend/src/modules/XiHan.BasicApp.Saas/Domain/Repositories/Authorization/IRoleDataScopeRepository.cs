// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色数据范围仓储接口
/// </summary>
public interface IRoleDataScopeRepository : ISaasRepository<SysRoleDataScope>
{
    /// <summary>
    /// 获取角色有效自定义数据范围
    /// </summary>
    Task<IReadOnlyList<SysRoleDataScope>> GetValidByRoleIdsAsync(IEnumerable<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}
