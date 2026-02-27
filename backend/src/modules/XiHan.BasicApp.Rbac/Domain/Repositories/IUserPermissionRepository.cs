#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserPermissionRepository
// Guid:bed79c73-70de-44d5-bf75-369af6decf27
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:35:22
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户直授权限关系仓储接口
/// </summary>
public interface IUserPermissionRepository : IRepositoryBase<SysUserPermission, long>
{
    /// <summary>
    /// 获取用户直授权限
    /// </summary>
    Task<IReadOnlyList<SysUserPermission>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户直授权限
    /// </summary>
    Task<bool> RemoveByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
