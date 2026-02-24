#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysUserRoleRepository
// Guid:3b1e1231-070f-47d2-a751-d5d44e538628
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 系统用户角色仓储接口
/// </summary>
public interface ISysUserRoleRepository : IReadOnlyRepositoryBase<SysUserRole, long>
{
    /// <summary>
    /// 根据用户ID获取用户角色列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色列表</returns>
    Task<List<SysUserRole>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据角色ID获取用户角色列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色列表</returns>
    Task<List<SysUserRole>> GetByRoleIdAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户的所有角色
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteByUserIdAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存用户角色
    /// </summary>
    /// <param name="userRole">用户角色实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>保存的用户角色实体</returns>
    Task<SysUserRole> SaveAsync(SysUserRole userRole, CancellationToken cancellationToken = default);
}
