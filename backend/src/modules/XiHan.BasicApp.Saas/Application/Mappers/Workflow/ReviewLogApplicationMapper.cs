// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// 审查日志应用层映射器
/// </summary>
public static class ReviewLogApplicationMapper
{
    /// <summary>
    /// 映射审查日志列表项
    /// </summary>
    /// <param name="reviewLog">审查日志实体</param>
    /// <returns>审查日志列表项 DTO</returns>
    public static ReviewLogListItemDto ToListItemDto(SysReviewLog reviewLog)
    {
        ArgumentNullException.ThrowIfNull(reviewLog);

        return new ReviewLogListItemDto
        {
            BasicId = reviewLog.BasicId,
            ReviewId = reviewLog.ReviewId,
            ReviewLevel = reviewLog.ReviewLevel,
            ReviewUserId = reviewLog.ReviewUserId,
            OriginalStatus = reviewLog.OriginalStatus,
            NewStatus = reviewLog.NewStatus,
            ReviewResult = reviewLog.ReviewResult,
            ReviewAction = reviewLog.ReviewAction,
            ReviewTime = reviewLog.ReviewTime,
            CreatedTime = reviewLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射审查日志详情
    /// </summary>
    /// <param name="reviewLog">审查日志实体</param>
    /// <returns>审查日志详情 DTO</returns>
    public static ReviewLogDetailDto ToDetailDto(SysReviewLog reviewLog)
    {
        ArgumentNullException.ThrowIfNull(reviewLog);

        var item = ToListItemDto(reviewLog);
        return new ReviewLogDetailDto
        {
            BasicId = item.BasicId,
            ReviewId = item.ReviewId,
            ReviewLevel = item.ReviewLevel,
            ReviewUserId = item.ReviewUserId,
            OriginalStatus = item.OriginalStatus,
            NewStatus = item.NewStatus,
            ReviewResult = item.ReviewResult,
            ReviewAction = item.ReviewAction,
            ReviewTime = item.ReviewTime,
            CreatedTime = item.CreatedTime,
            CreatedId = reviewLog.CreatedId,
            CreatedBy = reviewLog.CreatedBy
        };
    }
}
