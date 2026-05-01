#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewLogPageQueryDto
// Guid:36f8742c-bc7d-4469-b6df-8930fbb1652d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 审查日志分页查询 DTO
/// </summary>
public sealed class ReviewLogPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 审查主键
    /// </summary>
    public long? ReviewId { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    public int? ReviewLevel { get; set; }

    /// <summary>
    /// 审查人主键
    /// </summary>
    public long? ReviewUserId { get; set; }

    /// <summary>
    /// 原审查状态
    /// </summary>
    public AuditStatus? OriginalStatus { get; set; }

    /// <summary>
    /// 新审查状态
    /// </summary>
    public AuditStatus? NewStatus { get; set; }

    /// <summary>
    /// 审查结果
    /// </summary>
    public AuditResult? ReviewResult { get; set; }

    /// <summary>
    /// 审查动作
    /// </summary>
    public string? ReviewAction { get; set; }

    /// <summary>
    /// 审查开始时间
    /// </summary>
    public DateTimeOffset? ReviewTimeStart { get; set; }

    /// <summary>
    /// 审查结束时间
    /// </summary>
    public DateTimeOffset? ReviewTimeEnd { get; set; }
}
