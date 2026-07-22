// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 约束规则创建 DTO
/// </summary>
public sealed class ConstraintRuleCreateDto
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
    public ConstraintType ConstraintType { get; set; } = ConstraintType.SSD;

    /// <summary>
    /// 目标类型
    /// </summary>
    public ConstraintTargetType TargetType { get; set; } = ConstraintTargetType.Role;

    /// <summary>
    /// 约束参数
    /// </summary>
    public string? Parameters { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 违规处理方式
    /// </summary>
    public ViolationAction ViolationAction { get; set; } = ViolationAction.Deny;

    /// <summary>
    /// 规则描述
    /// </summary>
    public string? Description { get; set; }

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
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 规则项
    /// </summary>
    public IReadOnlyList<ConstraintRuleItemInputDto> Items { get; set; } = [];
}
