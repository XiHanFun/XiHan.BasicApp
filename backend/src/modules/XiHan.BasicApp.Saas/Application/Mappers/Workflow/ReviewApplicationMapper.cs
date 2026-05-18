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
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 系统审查应用层映射器
/// </summary>
public static class ReviewApplicationMapper
{
    /// <summary>
    /// 映射审查审核命令
    /// </summary>
    /// <param name="input">审查审核 DTO</param>
    /// <param name="currentUserId">当前用户标识</param>
    /// <returns>审查审核命令</returns>
    public static ReviewAuditCommand ToAuditCommand(ReviewAuditDto input, long? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ReviewAuditCommand(
            input.BasicId,
            input.ReviewResult,
            input.ReviewUserId,
            input.NextReviewUserId,
            input.ReviewComment,
            input.ReviewAction,
            input.ReviewTime,
            input.Attachments,
            input.ExtendData,
            input.Remark,
            currentUserId);
    }

    /// <summary>
    /// 映射审查创建命令
    /// </summary>
    /// <param name="input">审查创建 DTO</param>
    /// <param name="currentUserId">当前用户标识</param>
    /// <returns>审查创建命令</returns>
    public static ReviewCreateCommand ToCreateCommand(ReviewCreateDto input, long? currentUserId)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ReviewCreateCommand(
            input.ReviewCode,
            input.ReviewTitle,
            input.ReviewType,
            input.ReviewContent,
            input.ReviewDescription,
            input.EntityType,
            input.EntityId,
            input.BusinessData,
            input.ReviewStatus,
            input.ReviewResult,
            input.Priority,
            input.SubmitUserId,
            input.SubmitTime,
            input.CurrentReviewUserId,
            input.ReviewUserIds,
            input.ReviewLevel,
            input.CurrentLevel,
            input.ReviewStartTime,
            input.ReviewEndTime,
            input.Attachments,
            input.ExtendData,
            input.Status,
            input.Remark,
            currentUserId);
    }

    /// <summary>
    /// 映射审查更新命令
    /// </summary>
    /// <param name="input">审查更新 DTO</param>
    /// <returns>审查更新命令</returns>
    public static ReviewUpdateCommand ToUpdateCommand(ReviewUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ReviewUpdateCommand(
            input.BasicId,
            input.ReviewTitle,
            input.ReviewType,
            input.ReviewContent,
            input.ReviewDescription,
            input.EntityType,
            input.EntityId,
            input.BusinessData,
            input.Priority,
            input.SubmitUserId,
            input.SubmitTime,
            input.CurrentReviewUserId,
            input.ReviewUserIds,
            input.ReviewLevel,
            input.CurrentLevel,
            input.ReviewStartTime,
            input.ReviewEndTime,
            input.Attachments,
            input.ExtendData,
            input.Remark);
    }

    /// <summary>
    /// 映射审查状态变更命令
    /// </summary>
    /// <param name="input">审查状态变更 DTO</param>
    /// <returns>审查状态变更命令</returns>
    public static ReviewStatusChangeCommand ToStatusCommand(ReviewStatusUpdateDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ReviewStatusChangeCommand(input.BasicId, input.Status, input.Remark);
    }

    /// <summary>
    /// 映射审查撤回命令
    /// </summary>
    /// <param name="input">审查撤回 DTO</param>
    /// <returns>审查撤回命令</returns>
    public static ReviewWithdrawCommand ToWithdrawCommand(ReviewWithdrawDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return new ReviewWithdrawCommand(input.BasicId, input.Reason, input.WithdrawTime);
    }

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
