#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleDataScopeQueryService
// Guid:a78dd149-76ee-4b31-834a-3b61d21486a8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色数据范围查询应用服务接口
/// </summary>
public interface IRoleDataScopeQueryService : IApplicationService
{
    /// <summary>
    /// 获取角色数据范围列表
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="onlyValid">是否仅返回当前有效数据范围</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围列表</returns>
    Task<IReadOnlyList<RoleDataScopeListItemDto>> GetRoleDataScopesAsync(long roleId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取角色数据范围详情
    /// </summary>
    /// <param name="id">角色数据范围主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    Task<RoleDataScopeDetailDto?> GetRoleDataScopeDetailAsync(long id, CancellationToken cancellationToken = default);
}
