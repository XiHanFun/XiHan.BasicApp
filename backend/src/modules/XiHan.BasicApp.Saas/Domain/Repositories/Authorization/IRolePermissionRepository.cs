// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 角色权限仓储接口
/// </summary>
public interface IRolePermissionRepository : ISaasRepository<SysRolePermission>
{
    /// <summary>
    /// 获取角色有效权限授权
    /// </summary>
    Task<IReadOnlyList<SysRolePermission>> GetValidByRoleIdsAsync(IEnumerable<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken = default);
}
