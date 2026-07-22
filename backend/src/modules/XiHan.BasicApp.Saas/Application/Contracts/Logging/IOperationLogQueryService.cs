// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 操作日志查询应用服务接口
/// </summary>
public interface IOperationLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取操作日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志分页列表</returns>
    Task<PageResultDtoBase<OperationLogListItemDto>> GetOperationLogPageAsync(OperationLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取操作日志详情
    /// </summary>
    /// <param name="id">操作日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志详情</returns>
    Task<OperationLogDetailDto?> GetOperationLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
