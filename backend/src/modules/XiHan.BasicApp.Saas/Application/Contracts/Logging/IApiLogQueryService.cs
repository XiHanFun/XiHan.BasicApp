// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// API 日志查询应用服务接口
/// </summary>
public interface IApiLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取 API 日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API 日志分页列表</returns>
    Task<PageResultDtoBase<ApiLogListItemDto>> GetApiLogPageAsync(ApiLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 API 日志详情
    /// </summary>
    /// <param name="id">API 日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>API 日志详情</returns>
    Task<ApiLogDetailDto?> GetApiLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
