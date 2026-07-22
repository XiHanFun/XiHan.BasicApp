// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 资源查询应用服务接口
/// </summary>
public interface IResourceQueryService : IApplicationService
{
    /// <summary>
    /// 获取资源分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源分页列表</returns>
    Task<PageResultDtoBase<ResourceListItemDto>> GetResourcePageAsync(ResourcePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取资源详情
    /// </summary>
    /// <param name="id">资源主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源详情</returns>
    Task<ResourceDetailDto?> GetResourceDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可选全局资源列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可选全局资源列表</returns>
    Task<IReadOnlyList<ResourceSelectItemDto>> GetAvailableGlobalResourcesAsync(ResourceSelectQueryDto input, CancellationToken cancellationToken = default);
}
