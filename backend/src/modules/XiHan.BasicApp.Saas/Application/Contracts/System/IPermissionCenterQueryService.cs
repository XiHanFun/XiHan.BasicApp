#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionCenterQueryService
// Guid:f4b88ee2-6a86-4974-ab4c-a9680149b9cf
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限中心页面查询应用服务接口
/// </summary>
public interface IPermissionCenterQueryService : IApplicationService
{
    /// <summary>
    /// 获取权限中心详情聚合视图
    /// </summary>
    /// <param name="permissionId">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限中心详情聚合视图</returns>
    Task<PermissionCenterDetailDto?> GetPermissionCenterDetailAsync(long permissionId, CancellationToken cancellationToken = default);
}
