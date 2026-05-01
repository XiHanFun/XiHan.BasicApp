#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewListItemDto
// Guid:1f60f861-00c8-4262-8d17-bce34c5f2f20
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
    /// 是否包含摘要文本
    /// </summary>
    public bool HasSummaryText { get; set; }

    /// <summary>
    /// 是否包含审查载荷
    /// </summary>
    public bool HasReviewPayload { get; set; }

    /// <summary>
    /// 是否包含业务快照
    /// </summary>
    public bool HasBusinessSnapshot { get; set; }

    /// <summary>
    /// 是否包含审查人集合
    /// </summary>
    public bool HasReviewerSet { get; set; }

    /// <summary>
    /// 是否包含附件
    /// </summary>
    public bool HasAttachment { get; set; }

    /// <summary>
    /// 是否包含扩展数据
    /// </summary>
    public bool HasExtension { get; set; }

    /// <summary>
    /// 是否包含备注
    /// </summary>
    public bool HasNote { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
