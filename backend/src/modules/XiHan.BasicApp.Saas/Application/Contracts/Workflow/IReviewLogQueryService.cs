#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewLogQueryService
// Guid:64df9cf5-7200-45a7-8951-8cf76498fcd9
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
/// 审查日志查询应用服务接口
/// </summary>
public interface IReviewLogQueryService : IApplicationService
{
    /// <summary>
    /// 获取审查日志分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志分页列表</returns>
    Task<PageResultDtoBase<ReviewLogListItemDto>> GetReviewLogPageAsync(ReviewLogPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取审查日志详情
    /// </summary>
    /// <param name="id">审查日志主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>审查日志详情</returns>
    Task<ReviewLogDetailDto?> GetReviewLogDetailAsync(long id, CancellationToken cancellationToken = default);
}
