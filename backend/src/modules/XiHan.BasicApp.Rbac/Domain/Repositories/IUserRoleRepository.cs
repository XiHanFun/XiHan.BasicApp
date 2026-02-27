#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRoleRepository
// Guid:667afbab-954c-4c7a-b31b-8d58da6d7755
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:34:57
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户角色关系仓储接口
/// </summary>
public interface IUserRoleRepository : IRepositoryBase<SysUserRole, long>
{
    /// <summary>
    /// 获取用户角色关系
    /// </summary>
    Task<IReadOnlyList<SysUserRole>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户所有角色关系
    /// </summary>
    Task<bool> RemoveByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
