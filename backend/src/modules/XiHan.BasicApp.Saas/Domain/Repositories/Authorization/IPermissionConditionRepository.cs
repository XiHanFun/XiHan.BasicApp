// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 权限条件仓储接口
/// </summary>
public interface IPermissionConditionRepository : ISaasRepository<SysPermissionCondition>
{
    /// <summary>
    /// 获取角色或用户授权关联的有效 ABAC 条件
    /// </summary>
    Task<IReadOnlyList<SysPermissionCondition>> GetValidByAuthorizationIdsAsync(IEnumerable<long> rolePermissionIds, IEnumerable<long> userPermissionIds, CancellationToken cancellationToken = default);
}
