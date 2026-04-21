#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintRuleDto
// Guid:42c8d801-c59f-4f8f-b483-6976bca8c03f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 约束规则 DTO
/// </summary>
public class ConstraintRuleDto : BasicAppDto
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 规则参数
    /// </summary>
    public string Parameters { get; set; } = "{}";

    /// <summary>
    /// 是否平台级全局规则
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 违规处理方式
    /// </summary>
    public ViolationAction ViolationAction { get; set; } = ViolationAction.Deny;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveFrom { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 创建约束规则 DTO
/// </summary>
public class ConstraintRuleCreateDto : BasicAppCDto
{
    /// <summary>
    /// 规则编码
    /// </summary>
    [Required(ErrorMessage = "规则编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "规则编码长度必须在 1～100 之间")]
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称
    /// </summary>
    [Required(ErrorMessage = "规则名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "规则名称长度必须在 1～200 之间")]
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
    /// 是否平台级全局规则
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 规则参数
    /// </summary>
    [Required(ErrorMessage = "规则参数不能为空")]
    public string Parameters { get; set; } = "{}";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 违规处理方式
    /// </summary>
    public ViolationAction ViolationAction { get; set; } = ViolationAction.Deny;

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(1000, ErrorMessage = "描述长度不能超过 1000")]
    public string? Description { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveFrom { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新约束规则 DTO
/// </summary>
public class ConstraintRuleUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 规则编码
    /// </summary>
    [Required(ErrorMessage = "规则编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "规则编码长度必须在 1～100 之间")]
    public string RuleCode { get; set; } = string.Empty;

    /// <summary>
    /// 规则名称
    /// </summary>
    [Required(ErrorMessage = "规则名称不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "规则名称长度必须在 1～200 之间")]
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
    /// 是否平台级全局规则
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 规则参数
    /// </summary>
    [Required(ErrorMessage = "规则参数不能为空")]
    public string Parameters { get; set; } = "{}";

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 违规处理方式
    /// </summary>
    public ViolationAction ViolationAction { get; set; } = ViolationAction.Deny;

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(1000, ErrorMessage = "描述长度不能超过 1000")]
    public string? Description { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// 生效时间
    /// </summary>
    public DateTimeOffset? EffectiveFrom { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
