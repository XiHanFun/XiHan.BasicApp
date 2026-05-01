#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewLogApplicationMapper
// Guid:20ca917f-3d68-4a54-b75d-73a7b14cc680
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
            HasDecisionNote = !string.IsNullOrWhiteSpace(reviewLog.ReviewComment),
            HasAttachment = !string.IsNullOrWhiteSpace(reviewLog.Attachments),
            HasOperationContext = !string.IsNullOrWhiteSpace(reviewLog.ReviewIp),
            HasExtension = !string.IsNullOrWhiteSpace(reviewLog.ExtendData),
            HasNote = !string.IsNullOrWhiteSpace(reviewLog.Remark),
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
            HasDecisionNote = item.HasDecisionNote,
            HasAttachment = item.HasAttachment,
            HasOperationContext = item.HasOperationContext,
            HasExtension = item.HasExtension,
            HasNote = item.HasNote,
            CreatedTime = item.CreatedTime,
            CreatedId = reviewLog.CreatedId,
            CreatedBy = reviewLog.CreatedBy
        };
    }
}
