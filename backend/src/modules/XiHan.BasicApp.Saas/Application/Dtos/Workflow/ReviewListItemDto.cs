// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统审查列表项 DTO
/// </summary>
public class ReviewListItemDto : BasicAppDto
{
    /// <summary>
    /// 审查编码
    /// </summary>
    public string ReviewCode { get; set; } = string.Empty;

    /// <summary>
    /// 审查标题
    /// </summary>
    public string ReviewTitle { get; set; } = string.Empty;

    /// <summary>
    /// 审查类型
    /// </summary>
    public string ReviewType { get; set; } = string.Empty;

    /// <summary>
    /// 业务实体类型
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 业务实体 ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// 审查状态
    /// </summary>
    public AuditStatus ReviewStatus { get; set; }

    /// <summary>
    /// 审查结果
    /// </summary>
    public AuditResult? ReviewResult { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 提交人主键
    /// </summary>
    public long? SubmitUserId { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    public DateTimeOffset SubmitTime { get; set; }

    /// <summary>
    /// 当前审查人主键
    /// </summary>
    public long? CurrentReviewUserId { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    public int ReviewLevel { get; set; }

    /// <summary>
    /// 当前审查级别
    /// </summary>
    public int CurrentLevel { get; set; }

    /// <summary>
    /// 审查开始时间
    /// </summary>
    public DateTimeOffset? ReviewStartTime { get; set; }

    /// <summary>
    /// 审查结束时间
    /// </summary>
    public DateTimeOffset? ReviewEndTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
