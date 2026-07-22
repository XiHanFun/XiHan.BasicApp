// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
