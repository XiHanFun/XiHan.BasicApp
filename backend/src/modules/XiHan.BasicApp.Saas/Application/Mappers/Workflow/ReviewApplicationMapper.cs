#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewApplicationMapper
// Guid:01a9726b-afd8-4850-8067-a5c7a879a60d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统审查应用层映射器
/// </summary>
public static class ReviewApplicationMapper
{
    /// <summary>
    /// 映射系统审查列表项
    /// </summary>
    /// <param name="review">系统审查实体</param>
    /// <returns>系统审查列表项 DTO</returns>
    public static ReviewListItemDto ToListItemDto(SysReview review)
    {
        ArgumentNullException.ThrowIfNull(review);

        return new ReviewListItemDto
        {
            BasicId = review.BasicId,
            ReviewCode = review.ReviewCode,
            ReviewTitle = review.ReviewTitle,
            ReviewType = review.ReviewType,
            EntityType = review.EntityType,
            EntityId = review.EntityId,
            ReviewStatus = review.ReviewStatus,
            ReviewResult = review.ReviewResult,
            Priority = review.Priority,
            SubmitUserId = review.SubmitUserId,
            SubmitTime = review.SubmitTime,
            CurrentReviewUserId = review.CurrentReviewUserId,
            ReviewLevel = review.ReviewLevel,
            CurrentLevel = review.CurrentLevel,
            ReviewStartTime = review.ReviewStartTime,
            ReviewEndTime = review.ReviewEndTime,
            Status = review.Status,
            CreatedTime = review.CreatedTime,
            ModifiedTime = review.ModifiedTime
        };
    }

    /// <summary>
    /// 映射系统审查详情
    /// </summary>
    /// <param name="review">系统审查实体</param>
    /// <returns>系统审查详情 DTO</returns>
    public static ReviewDetailDto ToDetailDto(SysReview review)
    {
        ArgumentNullException.ThrowIfNull(review);

        var item = ToListItemDto(review);
        return new ReviewDetailDto
        {
            BasicId = item.BasicId,
            ReviewCode = item.ReviewCode,
            ReviewTitle = item.ReviewTitle,
            ReviewType = item.ReviewType,
            ReviewContent = review.ReviewContent,
            ReviewDescription = review.ReviewDescription,
            EntityType = item.EntityType,
            EntityId = item.EntityId,
            BusinessData = review.BusinessData,
            ReviewStatus = item.ReviewStatus,
            ReviewResult = item.ReviewResult,
            Priority = item.Priority,
            SubmitUserId = item.SubmitUserId,
            SubmitTime = item.SubmitTime,
            CurrentReviewUserId = item.CurrentReviewUserId,
            ReviewUserIds = review.ReviewUserIds,
            ReviewLevel = item.ReviewLevel,
            CurrentLevel = item.CurrentLevel,
            ReviewStartTime = item.ReviewStartTime,
            ReviewEndTime = item.ReviewEndTime,
            Attachments = review.Attachments,
            ExtendData = review.ExtendData,
            Status = item.Status,
            CreatedTime = item.CreatedTime,
            CreatedId = review.CreatedId,
            CreatedBy = review.CreatedBy,
            ModifiedTime = item.ModifiedTime,
            ModifiedId = review.ModifiedId,
            ModifiedBy = review.ModifiedBy,
            Remark = review.Remark
        };
    }
}
