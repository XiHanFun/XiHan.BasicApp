#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IResourceQueryService
// Guid:ca6dd368-2f88-4a62-932e-77f51750839a
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
