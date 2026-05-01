#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IReviewQueryService
// Guid:dc8d3dd2-bf6c-4b8f-a3db-4e869de452b8
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
/// 系统审查查询应用服务接口
/// </summary>
public interface IReviewQueryService : IApplicationService
{
    /// <summary>
    /// 获取系统审查分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统审查分页列表</returns>
    Task<PageResultDtoBase<ReviewListItemDto>> GetReviewPageAsync(ReviewPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统审查详情
    /// </summary>
    /// <param name="id">系统审查主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统审查详情</returns>
    Task<ReviewDetailDto?> GetReviewDetailAsync(long id, CancellationToken cancellationToken = default);
}
