#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewDto
// Guid:3505c182-c239-4786-8af6-a088dbef37f4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:32:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 审查 DTO
/// </summary>
public class ReviewDto : BasicAppDto
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
    /// 审查内容
    /// </summary>
    public string? ReviewContent { get; set; }

    /// <summary>
    /// 审查状态
    /// </summary>
    public AuditStatus ReviewStatus { get; set; } = AuditStatus.Pending;

    /// <summary>
    /// 审查结果
    /// </summary>
    public AuditResult? ReviewResult { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 提交用户标识
    /// </summary>
    public long? SubmitUserId { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    public DateTimeOffset SubmitTime { get; set; }

    /// <summary>
    /// 当前审查用户标识
    /// </summary>
    public long? CurrentReviewUserId { get; set; }

    /// <summary>
    /// 审查层级
    /// </summary>
    public int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 当前层级
    /// </summary>
    public int CurrentLevel { get; set; } = 1;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 创建审查 DTO
/// </summary>
public class ReviewCreateDto : BasicAppCDto
{
    /// <summary>
    /// 审查编码
    /// </summary>
    [Required(ErrorMessage = "审查编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "审查编码长度必须在 1～100 之间")]
    public string ReviewCode { get; set; } = string.Empty;

    /// <summary>
    /// 审查标题
    /// </summary>
    [Required(ErrorMessage = "审查标题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "审查标题长度必须在 1～200 之间")]
    public string ReviewTitle { get; set; } = string.Empty;

    /// <summary>
    /// 审查类型
    /// </summary>
    [Required(ErrorMessage = "审查类型不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "审查类型长度必须在 1～50 之间")]
    public string ReviewType { get; set; } = string.Empty;

    /// <summary>
    /// 审查内容
    /// </summary>
    public string? ReviewContent { get; set; }

    /// <summary>
    /// 审查描述
    /// </summary>
    [StringLength(1000, ErrorMessage = "审查描述长度不能超过 1000")]
    public string? ReviewDescription { get; set; }

    /// <summary>
    /// 业务实体类型
    /// </summary>
    [StringLength(100, ErrorMessage = "业务实体类型长度不能超过 100")]
    public string? EntityType { get; set; }

    /// <summary>
    /// 业务实体 ID
    /// </summary>
    [StringLength(100, ErrorMessage = "业务实体ID长度不能超过 100")]
    public string? EntityId { get; set; }

    /// <summary>
    /// 业务数据（JSON）
    /// </summary>
    public string? BusinessData { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    [Range(1, 5, ErrorMessage = "优先级范围为 1～5")]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 提交用户标识
    /// </summary>
    public long? SubmitUserId { get; set; }

    /// <summary>
    /// 提交人名称
    /// </summary>
    [StringLength(50, ErrorMessage = "提交人名称长度不能超过 50")]
    public string? SubmitUserName { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    public DateTimeOffset SubmitTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// 当前审查用户标识
    /// </summary>
    public long? CurrentReviewUserId { get; set; }

    /// <summary>
    /// 当前审查人名称
    /// </summary>
    [StringLength(50, ErrorMessage = "当前审查人名称长度不能超过 50")]
    public string? CurrentReviewUserName { get; set; }

    /// <summary>
    /// 审查人用户 ID 列表
    /// </summary>
    public string? ReviewUserIds { get; set; }

    /// <summary>
    /// 审查层级
    /// </summary>
    public int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 当前层级
    /// </summary>
    public int CurrentLevel { get; set; } = 1;

    /// <summary>
    /// 租户标识
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新审查 DTO
/// </summary>
public class ReviewUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 审查标题
    /// </summary>
    [Required(ErrorMessage = "审查标题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "审查标题长度必须在 1～200 之间")]
    public string ReviewTitle { get; set; } = string.Empty;

    /// <summary>
    /// 审查类型
    /// </summary>
    [Required(ErrorMessage = "审查类型不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "审查类型长度必须在 1～50 之间")]
    public string ReviewType { get; set; } = string.Empty;

    /// <summary>
    /// 审查内容
    /// </summary>
    public string? ReviewContent { get; set; }

    /// <summary>
    /// 审查描述
    /// </summary>
    [StringLength(1000, ErrorMessage = "审查描述长度不能超过 1000")]
    public string? ReviewDescription { get; set; }

    /// <summary>
    /// 业务实体类型
    /// </summary>
    [StringLength(100, ErrorMessage = "业务实体类型长度不能超过 100")]
    public string? EntityType { get; set; }

    /// <summary>
    /// 业务实体 ID
    /// </summary>
    [StringLength(100, ErrorMessage = "业务实体ID长度不能超过 100")]
    public string? EntityId { get; set; }

    /// <summary>
    /// 业务数据（JSON）
    /// </summary>
    public string? BusinessData { get; set; }

    /// <summary>
    /// 审查状态
    /// </summary>
    public AuditStatus ReviewStatus { get; set; } = AuditStatus.Pending;

    /// <summary>
    /// 审查结果
    /// </summary>
    public AuditResult? ReviewResult { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    [Range(1, 5, ErrorMessage = "优先级范围为 1～5")]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 当前审查用户标识
    /// </summary>
    public long? CurrentReviewUserId { get; set; }

    /// <summary>
    /// 当前审查人名称
    /// </summary>
    [StringLength(50, ErrorMessage = "当前审查人名称长度不能超过 50")]
    public string? CurrentReviewUserName { get; set; }

    /// <summary>
    /// 审查人用户 ID 列表
    /// </summary>
    public string? ReviewUserIds { get; set; }

    /// <summary>
    /// 审查层级
    /// </summary>
    public int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 当前层级
    /// </summary>
    public int CurrentLevel { get; set; } = 1;

    /// <summary>
    /// 审查意见
    /// </summary>
    [StringLength(1000, ErrorMessage = "审查意见长度不能超过 1000")]
    public string? ReviewComment { get; set; }

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
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
