#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionAuthorizationService
// Guid:c5d6e7f8-a9bc-def1-2345-67890abcdef1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Domain.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.DomainServices;

/// <summary>
/// 权限授权领域服务接口
/// </summary>
public interface IPermissionAuthorizationService : IDomainService
{
    /// <summary>
    /// 检查用户是否拥有指定权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有权限</returns>
    Task<bool> HasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否拥有多个权限中的任意一个
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有任意权限</returns>
    Task<bool> HasAnyPermissionAsync(long userId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否拥有所有指定权限
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="permissionCodes">权限编码列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有所有权限</returns>
    Task<bool> HasAllPermissionsAsync(long userId, IEnumerable<string> permissionCodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否可以访问指定资源
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="resourceCode">资源编码</param>
    /// <param name="operationCode">操作编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以访问</returns>
    Task<bool> CanAccessResourceAsync(long userId, string resourceCode, string operationCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查角色是否拥有指定权限
    /// </summary>
    /// <param name="roleId">角色ID</param>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否拥有权限</returns>
    Task<bool> RoleHasPermissionAsync(long roleId, string permissionCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有权限编码
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限编码列表</returns>
    Task<List<string>> GetUserPermissionCodesAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查权限是否处于活跃状态
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否活跃</returns>
    Task<bool> IsPermissionActiveAsync(string permissionCode, CancellationToken cancellationToken = default);
}
