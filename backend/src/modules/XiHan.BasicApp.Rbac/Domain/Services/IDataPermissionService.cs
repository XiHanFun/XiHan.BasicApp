#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDataPermissionService
// Guid:f8a9bcde-f123-4567-890a-bcdef1234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Domain.Services;

/// <summary>
/// 数据权限领域服务接口
/// </summary>
public interface IDataPermissionService : IDomainService
{
    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数据权限范围</returns>
    Task<DataPermissionScope> GetUserDataScopeAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户可访问的部门ID列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门ID列表</returns>
    Task<List<long>> GetAccessibleDepartmentIdsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否可以访问指定部门的数据
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以访问</returns>
    Task<bool> CanAccessDepartmentDataAsync(long userId, long departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否可以访问指定用户的数据
    /// </summary>
    /// <param name="currentUserId">当前用户ID</param>
    /// <param name="targetUserId">目标用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以访问</returns>
    Task<bool> CanAccessUserDataAsync(long currentUserId, long targetUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据角色获取数据权限范围
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>数据权限范围</returns>
    Task<DataPermissionScope> GetRoleDataScopeAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色的数据权限部门列表
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门ID列表</returns>
    Task<List<long>> GetRoleDataScopeDepartmentIdsAsync(long roleId, CancellationToken cancellationToken = default);
}
