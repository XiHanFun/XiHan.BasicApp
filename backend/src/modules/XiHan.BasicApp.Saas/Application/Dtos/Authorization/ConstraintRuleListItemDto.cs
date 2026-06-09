#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleListItemDto
// Guid:e3a6418a-a652-41cc-a034-3d81de8e4d47
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 约束规则列表项 DTO
/// </summary>
public sealed class ConstraintRuleListItemDto : BasicAppDto
{
    /// <summary>
    /// 规则编码
    /// </summary>
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// 约束类型
    /// </summary>
    public ConstraintType ConstraintType { get; set; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public ConstraintTargetType TargetType { get; set; }

    /// <summary>
    /// 是否平台级全局规则
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 当前是否生效
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 违规处理方式
    /// </summary>
    public ViolationAction ViolationAction { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 规则项数量
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// 规则描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
