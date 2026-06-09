#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserPermissionQueryService
// Guid:f8813c7e-dff9-4dbd-9534-751f73b69c47
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户直授权限查询应用服务接口
/// </summary>
public interface IUserPermissionQueryService : IApplicationService
{
    /// <summary>
    /// 获取用户直授权限列表
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="onlyValid">是否仅返回当前有效直授权限</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限列表</returns>
    Task<IReadOnlyList<UserPermissionListItemDto>> GetUserPermissionsAsync(long userId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户直授权限详情
    /// </summary>
    /// <param name="id">用户直授权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    Task<UserPermissionDetailDto?> GetUserPermissionDetailAsync(long id, CancellationToken cancellationToken = default);
}
