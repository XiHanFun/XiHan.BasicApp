// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 约束规则项 DTO
/// </summary>
public sealed class ConstraintRuleItemDto : BasicAppDto
{
    /// <summary>
    /// 目标类型
    /// </summary>
    public ConstraintTargetType TargetType { get; set; }

    /// <summary>
    /// 目标主键
    /// </summary>
    public long TargetId { get; set; }

    /// <summary>
    /// 目标编码
    /// </summary>
    public string? TargetCode { get; set; }

    /// <summary>
    /// 目标名称
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// 约束分组
    /// </summary>
    public int ConstraintGroup { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}
