#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPermissionQueryService
// Guid:53cce3dc-7e7f-46c2-87a8-969f97327a5f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 权限查询应用服务接口
/// </summary>
public interface IPermissionQueryService : IApplicationService
{
    /// <summary>
    /// 获取权限分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限分页列表</returns>
    Task<PageResultDtoBase<PermissionListItemDto>> GetPermissionPageAsync(PermissionPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取权限详情
    /// </summary>
    /// <param name="id">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    Task<PermissionDetailDto?> GetPermissionDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可选全局权限列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可选全局权限列表</returns>
    Task<IReadOnlyList<PermissionSelectItemDto>> GetAvailableGlobalPermissionsAsync(PermissionSelectQueryDto input, CancellationToken cancellationToken = default);
}
