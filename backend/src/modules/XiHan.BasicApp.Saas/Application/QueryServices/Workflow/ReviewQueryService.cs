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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
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
public sealed class ReviewQueryService
    : SaasApplicationService, IReviewQueryService
{
    /// <summary>
    /// 系统审查仓储
    /// </summary>
    private readonly IReviewRepository _reviewRepository;

    /// <summary>
    /// 字段级安全（读脱敏 / 写校验）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewQueryService(IReviewRepository reviewRepository, IFieldSecurityService fieldSecurityService)
    {
        _reviewRepository = reviewRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取系统审查分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统审查分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Review.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<ReviewListItemDto>> GetReviewPageAsync(ReviewPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildReviewPageRequest(input);
        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysReview", cancellationToken);
        // 过滤：FLS 门控剔除不可读/已脱敏字段（时间区间 Between / 枚举多选 In）
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysReview", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyReviewSorts(request);
        }

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
            request.Conditions.SetKeyword<SysReview>(
                input.Keyword.Trim(),
                review => review.ReviewCode,
                review => review.ReviewTitle,
                review => review.ReviewType,
                review => review.EntityType,
                review => review.EntityId);
        }

        if (!string.IsNullOrWhiteSpace(input.ReviewCode))
        {
            request.Conditions.AddFilter((SysReview review) => review.ReviewCode, input.ReviewCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.ReviewType))
        {
            request.Conditions.AddFilter((SysReview review) => review.ReviewType, input.ReviewType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.EntityType))
        {
            request.Conditions.AddFilter((SysReview review) => review.EntityType, input.EntityType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(input.EntityId))
        {
            request.Conditions.AddFilter((SysReview review) => review.EntityId, input.EntityId.Trim());
        }

        if (input.SubmitUserId.HasValue && input.SubmitUserId.Value > 0)
        {
            request.Conditions.AddFilter((SysReview review) => review.SubmitUserId, input.SubmitUserId.Value);
        }

        if (input.CurrentReviewUserId.HasValue && input.CurrentReviewUserId.Value > 0)
        {
            request.Conditions.AddFilter((SysReview review) => review.CurrentReviewUserId, input.CurrentReviewUserId.Value);
        }

        if (input.ReviewStatus.HasValue)
        {
            request.Conditions.AddFilter((SysReview review) => review.ReviewStatus, input.ReviewStatus.Value);
        }

        if (input.ReviewResult.HasValue)
        {
            request.Conditions.AddFilter((SysReview review) => review.ReviewResult, input.ReviewResult.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysReview review) => review.Status, input.Status.Value);
        }

        AddTimeRange(request, nameof(SysReview.SubmitTime), input.SubmitTimeStart, input.SubmitTimeEnd);
        AddTimeRange(request, nameof(SysReview.ReviewStartTime), input.ReviewStartTimeStart, input.ReviewStartTimeEnd);
        AddTimeRange(request, nameof(SysReview.ReviewEndTime), input.ReviewEndTimeStart, input.ReviewEndTimeEnd);
        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetReviewPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端下发的过滤原样带入（时间区间 / 枚举多选；FLS 门控在调用方处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }

    /// <summary>
    /// 施加系统审查默认排序（无前端排序时的兜底）
    /// </summary>
    private static void ApplyReviewSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysReview review) => review.Priority, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysReview review) => review.SubmitTime, SortDirection.Descending, 1);
        request.Conditions.AddSort((SysReview review) => review.CreatedTime, SortDirection.Descending, 2);
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
