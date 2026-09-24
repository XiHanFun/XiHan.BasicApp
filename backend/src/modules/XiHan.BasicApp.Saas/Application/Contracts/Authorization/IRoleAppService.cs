// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色命令应用服务接口
/// </summary>
public interface IRoleAppService : IApplicationService
{
    /// <summary>
    /// 创建角色
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    Task<RoleDetailDto> CreateRoleAsync(RoleCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    Task<RoleDetailDto> UpdateRoleAsync(RoleUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色详情</returns>
    Task<RoleDetailDto> UpdateRoleStatusAsync(RoleStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色
    /// </summary>
    /// <param name="id">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRoleAsync(long id, CancellationToken cancellationToken = default);

    #region RolePermission

    /// <summary>
    /// 批量变更角色权限（一次性提交授予与撤销，单事务）
    /// </summary>
    /// <param name="input">批量变更参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BatchUpdateRolePermissionsAsync(RolePermissionBatchUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    Task<RolePermissionDetailDto> UpdateRolePermissionAsync(RolePermissionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    Task<RolePermissionDetailDto> UpdateRolePermissionStatusAsync(RolePermissionStatusUpdateDto input, CancellationToken cancellationToken = default);

    #endregion RolePermission

    #region RoleDataScope

    /// <summary>
    /// 设置角色数据范围：档位与自定义部门一次提交（单事务）
    /// </summary>
    /// <param name="input">设置参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetRoleDataScopeAsync(RoleDataScopeSetDto input, CancellationToken cancellationToken = default);

    #endregion RoleDataScope

    #region RoleHierarchy

    /// <summary>
    /// 批量变更角色的直接父角色（一次性提交新增与移除）
    /// </summary>
    /// <param name="input">批量变更参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BatchUpdateRoleParentsAsync(RoleHierarchyBatchUpdateDto input, CancellationToken cancellationToken = default);

    #endregion RoleHierarchy
}
