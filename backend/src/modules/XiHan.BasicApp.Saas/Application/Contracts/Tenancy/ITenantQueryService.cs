// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 租户查询应用服务接口
/// </summary>
public interface ITenantQueryService : IApplicationService
{
    /// <summary>
    /// 获取租户分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户分页列表</returns>
    Task<PageResultDtoBase<TenantListItemDto>> GetTenantPageAsync(TenantPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户详情
    /// </summary>
    /// <param name="id">租户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户详情</returns>
    Task<TenantDetailDto?> GetTenantDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前用户可进入的租户列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>当前用户可进入的租户列表</returns>
    Task<IReadOnlyList<TenantSwitcherDto>> GetMyAvailableTenantsAsync(CancellationToken cancellationToken = default);
}
