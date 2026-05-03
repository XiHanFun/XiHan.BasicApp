#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExceptionLogQueryService
// Guid:0f931501-99dd-40eb-b310-9ab693d17963
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
