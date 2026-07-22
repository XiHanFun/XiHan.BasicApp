// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 账号管理页面查询应用服务接口
/// </summary>
public interface IUserManagementQueryService : IApplicationService
{
    /// <summary>
    /// 获取账号管理详情聚合视图
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>账号管理详情聚合视图</returns>
    Task<UserManagementDetailDto?> GetUserManagementDetailAsync(long userId, CancellationToken cancellationToken = default);
}
