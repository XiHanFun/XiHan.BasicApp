#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRoleManagementQueryService
// Guid:28c2c307-da8e-4263-a972-dbe6a0ebc014
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 角色管理页面查询应用服务接口
/// </summary>
public interface IRoleManagementQueryService : IApplicationService
{
    /// <summary>
    /// 获取角色管理详情聚合视图
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色管理详情聚合视图</returns>
    Task<RoleManagementDetailDto?> GetRoleManagementDetailAsync(long roleId, CancellationToken cancellationToken = default);
}
