// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
