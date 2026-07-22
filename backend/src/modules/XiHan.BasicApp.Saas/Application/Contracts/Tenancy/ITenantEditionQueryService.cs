// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 租户版本查询应用服务接口
/// </summary>
public interface ITenantEditionQueryService : IApplicationService
{
    /// <summary>
    /// 获取租户版本分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本分页列表</returns>
    Task<PageResultDtoBase<TenantEditionListItemDto>> GetTenantEditionPageAsync(TenantEditionPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取租户版本详情
    /// </summary>
    /// <param name="id">租户版本主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本详情</returns>
    Task<TenantEditionDetailDto?> GetTenantEditionDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取已启用租户版本列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已启用租户版本列表</returns>
    Task<IReadOnlyList<TenantEditionListItemDto>> GetEnabledTenantEditionsAsync(CancellationToken cancellationToken = default);
}
