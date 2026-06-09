#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleAppService
// Guid:ce77247d-9ae3-493b-a4fd-1634e1531721
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
    /// 授予角色权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色权限详情</returns>
    Task<RolePermissionDetailDto> CreateRolePermissionAsync(RolePermissionGrantDto input, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// 撤销角色权限
    /// </summary>
    /// <param name="id">角色权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRolePermissionAsync(long id, CancellationToken cancellationToken = default);

    #endregion RolePermission

    #region RoleDataScope

    /// <summary>
    /// 授予角色数据范围
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    Task<RoleDataScopeDetailDto> CreateRoleDataScopeAsync(RoleDataScopeGrantDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色数据范围
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    Task<RoleDataScopeDetailDto> UpdateRoleDataScopeAsync(RoleDataScopeUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色数据范围状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    Task<RoleDataScopeDetailDto> UpdateRoleDataScopeStatusAsync(RoleDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销角色数据范围
    /// </summary>
    /// <param name="id">角色数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRoleDataScopeAsync(long id, CancellationToken cancellationToken = default);

    #endregion RoleDataScope

    #region RoleHierarchy

    /// <summary>
    /// 创建角色直接继承关系
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色继承详情</returns>
    Task<RoleHierarchyDetailDto> CreateRoleHierarchyAsync(RoleHierarchyCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色直接继承关系
    /// </summary>
    /// <param name="id">角色继承主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteRoleHierarchyAsync(long id, CancellationToken cancellationToken = default);

    #endregion RoleHierarchy
}
