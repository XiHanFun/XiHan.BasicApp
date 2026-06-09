#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRolePermissionRepository
// Guid:d15557a5-5c71-4a17-b4df-8b7d181bc8dd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
