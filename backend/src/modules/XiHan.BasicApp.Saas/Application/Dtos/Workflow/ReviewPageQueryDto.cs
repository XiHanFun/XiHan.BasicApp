#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewPageQueryDto
// Guid:ff410980-0246-4753-ab74-c09bd92f4b4a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统审查分页查询 DTO
/// </summary>
public sealed class ReviewPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 审查编码
    /// </summary>
    public string? ReviewCode { get; set; }

    /// <summary>
    /// 审查类型
    /// </summary>
    public string? ReviewType { get; set; }

    /// <summary>
    /// 业务实体类型
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 业务实体 ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// 提交人主键
    /// </summary>
    public long? SubmitUserId { get; set; }

    /// <summary>
    /// 当前审查人主键
    /// </summary>
    public long? CurrentReviewUserId { get; set; }

    /// <summary>
    /// 审查状态
    /// </summary>
    public AuditStatus? ReviewStatus { get; set; }

    /// <summary>
    /// 审查结果
    /// </summary>
    public AuditResult? ReviewResult { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }

    /// <summary>
    /// 提交时间起点
    /// </summary>
    public DateTimeOffset? SubmitTimeStart { get; set; }

    /// <summary>
    /// 提交时间终点
    /// </summary>
    public DateTimeOffset? SubmitTimeEnd { get; set; }

    /// <summary>
    /// 审查开始时间起点
    /// </summary>
    public DateTimeOffset? ReviewStartTimeStart { get; set; }

    /// <summary>
    /// 审查开始时间终点
    /// </summary>
    public DateTimeOffset? ReviewStartTimeEnd { get; set; }

    /// <summary>
    /// 审查结束时间起点
    /// </summary>
    public DateTimeOffset? ReviewEndTimeStart { get; set; }

    /// <summary>
    /// 审查结束时间终点
    /// </summary>
    public DateTimeOffset? ReviewEndTimeEnd { get; set; }
}
