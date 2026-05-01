#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewLogListItemDto
// Guid:a65f8dd5-bf40-4857-83c4-2b241a519dd7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 审查日志列表项 DTO
/// </summary>
public class ReviewLogListItemDto : BasicAppDto
{
    /// <summary>
    /// 审查主键
    /// </summary>
    public long ReviewId { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    public int ReviewLevel { get; set; }

    /// <summary>
    /// 审查人主键
    /// </summary>
    public long? ReviewUserId { get; set; }

    /// <summary>
    /// 原审查状态
    /// </summary>
    public AuditStatus OriginalStatus { get; set; }

    /// <summary>
    /// 新审查状态
    /// </summary>
    public AuditStatus NewStatus { get; set; }

    /// <summary>
    /// 审查结果
    /// </summary>
    public AuditResult ReviewResult { get; set; }

    /// <summary>
    /// 审查动作
    /// </summary>
    public string? ReviewAction { get; set; }

    /// <summary>
    /// 审查时间
    /// </summary>
    public DateTimeOffset ReviewTime { get; set; }

    /// <summary>
    /// 是否包含决策说明
    /// </summary>
    public bool HasDecisionNote { get; set; }

    /// <summary>
    /// 是否包含附件
    /// </summary>
    public bool HasAttachment { get; set; }

    /// <summary>
    /// 是否包含操作上下文
    /// </summary>
    public bool HasOperationContext { get; set; }

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
}
