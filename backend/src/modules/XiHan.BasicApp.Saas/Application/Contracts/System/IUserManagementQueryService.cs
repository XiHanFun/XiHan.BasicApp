#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserManagementQueryService
// Guid:6adfc409-ac07-4ba9-8cd3-9fc62760ad24
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
