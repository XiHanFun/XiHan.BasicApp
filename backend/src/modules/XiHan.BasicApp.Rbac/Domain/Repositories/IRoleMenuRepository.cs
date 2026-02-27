#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleMenuRepository
// Guid:2652b16c-e0ea-4cc6-83b9-7cec07876c1f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:35:14
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 角色菜单关系仓储接口
/// </summary>
public interface IRoleMenuRepository : IRepositoryBase<SysRoleMenu, long>
{
    /// <summary>
    /// 获取角色菜单关系
    /// </summary>
    Task<IReadOnlyList<SysRoleMenu>> GetByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色菜单关系
    /// </summary>
    Task<bool> RemoveByRoleIdAsync(long roleId, long? tenantId = null, CancellationToken cancellationToken = default);
}
