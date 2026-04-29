#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserRoleQueryService
// Guid:2c856cf6-f3cb-498c-b3a2-d28172fb08e2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户角色查询应用服务接口
/// </summary>
public interface IUserRoleQueryService : IApplicationService
{
    /// <summary>
    /// 获取用户角色列表
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="onlyValid">是否仅返回当前有效角色授权</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色列表</returns>
    Task<IReadOnlyList<UserRoleListItemDto>> GetUserRolesAsync(long userId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户角色详情
    /// </summary>
    /// <param name="id">用户角色绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    Task<UserRoleDetailDto?> GetUserRoleDetailAsync(long id, CancellationToken cancellationToken = default);
}
