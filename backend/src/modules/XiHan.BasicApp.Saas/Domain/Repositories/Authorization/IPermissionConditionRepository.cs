#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionConditionRepository
// Guid:7198b789-f777-4797-9529-d7d794805ba8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
