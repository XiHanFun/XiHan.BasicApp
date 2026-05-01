#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewQueryService
// Guid:0fbf1b90-a974-46b8-844f-3759b4d9e6aa
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 系统审查查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统审查")]
public sealed class ReviewQueryService(IReviewRepository reviewRepository)
    : SaasApplicationService, IReviewQueryService
{
    /// <summary>
    /// 系统审查仓储
    /// </summary>
    private readonly IReviewRepository _reviewRepository = reviewRepository;

    /// <summary>
    /// 获取系统审查分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统审查分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Review.Read)]
    public async Task<PageResultDtoBase<ReviewListItemDto>> GetReviewPageAsync(ReviewPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildReviewPageRequest(input);
        var reviews = await _reviewRepository.GetPagedAsync(request, cancellationToken);
        return reviews.Map(ReviewApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取系统审查详情
    /// </summary>
    /// <param name="id">系统审查主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统审查详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Review.Read)]
    public async Task<ReviewDetailDto?> GetReviewDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统审查主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var review = await _reviewRepository.GetByIdAsync(id, cancellationToken);
        return review is null ? null : ReviewApplicationMapper.ToDetailDto(review);
    }

    /// <summary>
    /// 构建系统审查分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统审查分页请求</returns>
    private static BasicAppPRDto BuildReviewPageRequest(ReviewPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysReview.ReviewCode),
                nameof(SysReview.ReviewTitle),
                nameof(SysReview.ReviewType),
                nameof(SysReview.EntityType),
                nameof(SysReview.EntityId));
        }

        if (!string.IsNullOrWhiteSpace(input.ReviewCode))
        {
            request.Conditions.AddFilter(nameof(SysReview.ReviewCode), input.ReviewCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.ReviewType))
        {
            request.Conditions.AddFilter(nameof(SysReview.ReviewType), input.ReviewType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.EntityType))
        {
            request.Conditions.AddFilter(nameof(SysReview.EntityType), input.EntityType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.EntityId))
        {
            request.Conditions.AddFilter(nameof(SysReview.EntityId), input.EntityId.Trim());
        }

        if (input.SubmitUserId.HasValue && input.SubmitUserId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysReview.SubmitUserId), input.SubmitUserId.Value);
        }

        if (input.CurrentReviewUserId.HasValue && input.CurrentReviewUserId.Value > 0)
        {
            request.Conditions.AddFilter(nameof(SysReview.CurrentReviewUserId), input.CurrentReviewUserId.Value);
        }

        if (input.ReviewStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysReview.ReviewStatus), input.ReviewStatus.Value);
        }

        if (input.ReviewResult.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysReview.ReviewResult), input.ReviewResult.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysReview.Status), input.Status.Value);
        }

        AddTimeRange(request, nameof(SysReview.SubmitTime), input.SubmitTimeStart, input.SubmitTimeEnd);
        AddTimeRange(request, nameof(SysReview.ReviewStartTime), input.ReviewStartTimeStart, input.ReviewStartTimeEnd);
        AddTimeRange(request, nameof(SysReview.ReviewEndTime), input.ReviewEndTimeStart, input.ReviewEndTimeEnd);
        request.Conditions.AddSort(nameof(SysReview.Priority), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysReview.SubmitTime), SortDirection.Descending, 1);
        request.Conditions.AddSort(nameof(SysReview.CreatedTime), SortDirection.Descending, 2);
        return request;
    }

    /// <summary>
    /// 添加时间范围筛选
    /// </summary>
    private static void AddTimeRange(BasicAppPRDto request, string fieldName, DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start.HasValue)
        {
            request.Conditions.AddFilter(fieldName, start.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (end.HasValue)
        {
            request.Conditions.AddFilter(fieldName, end.Value, QueryOperator.LessThanOrEqual);
        }
    }
}
