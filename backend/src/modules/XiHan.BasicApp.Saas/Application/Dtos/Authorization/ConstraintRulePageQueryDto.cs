#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRulePageQueryDto
// Guid:549ccead-0d6e-4537-8121-127d38826bda
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
/// 约束规则分页查询 DTO
/// </summary>
public sealed class ConstraintRulePageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（规则编码、名称、描述、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 约束类型
    /// </summary>
    public ConstraintType? ConstraintType { get; set; }

    /// <summary>
    /// 目标类型
    /// </summary>
    public ConstraintTargetType? TargetType { get; set; }

    /// <summary>
    /// 违规处理方式
    /// </summary>
    public ViolationAction? ViolationAction { get; set; }

    /// <summary>
    /// 是否平台级全局规则
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
