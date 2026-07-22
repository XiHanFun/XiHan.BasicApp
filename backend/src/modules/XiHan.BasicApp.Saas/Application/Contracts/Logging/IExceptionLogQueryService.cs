// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 异常日志查询应用服务接口
/// </summary>
public interface IExceptionLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取异常日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异常日志分页列表</returns>
    Task<PageResultDtoBase<ExceptionLogListItemDto>> GetExceptionLogPageAsync(ExceptionLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取异常日志详情
    /// </summary>
    /// <param name="id">异常日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异常日志详情</returns>
    Task<ExceptionLogDetailDto?> GetExceptionLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
