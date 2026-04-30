#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionConditionQueryService
// Guid:d6f21f04-59bd-42d8-a090-392259dbf7d9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限 ABAC 条件查询应用服务接口
/// </summary>
public interface IPermissionConditionQueryService : IApplicationService
{
    /// <summary>
    /// 获取角色权限绑定的 ABAC 条件
    /// </summary>
    /// <param name="rolePermissionId">角色权限绑定主键</param>
    /// <param name="onlyValid">是否仅返回有效条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件列表</returns>
    Task<IReadOnlyList<PermissionConditionListItemDto>> GetRolePermissionConditionsAsync(long rolePermissionId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户直授权限绑定的 ABAC 条件
    /// </summary>
    /// <param name="userPermissionId">用户直授权限绑定主键</param>
    /// <param name="onlyValid">是否仅返回有效条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件列表</returns>
    Task<IReadOnlyList<PermissionConditionListItemDto>> GetUserPermissionConditionsAsync(long userPermissionId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取权限 ABAC 条件详情
    /// </summary>
    /// <param name="id">ABAC 条件主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>ABAC 条件详情</returns>
    Task<PermissionConditionDetailDto?> GetPermissionConditionDetailAsync(long id, CancellationToken cancellationToken = default);
}
