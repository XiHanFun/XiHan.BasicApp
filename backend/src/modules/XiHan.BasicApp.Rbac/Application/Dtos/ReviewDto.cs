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
    public string ReviewCode { get; set; } = string.Empty;

    public string ReviewTitle { get; set; } = string.Empty;

    public string ReviewType { get; set; } = string.Empty;

    public string? ReviewContent { get; set; }

    public AuditStatus ReviewStatus { get; set; } = AuditStatus.Pending;

    public AuditResult? ReviewResult { get; set; }

    public int Priority { get; set; } = 3;

    public long? SubmitUserId { get; set; }

    public DateTimeOffset SubmitTime { get; set; }

    public long? CurrentReviewUserId { get; set; }

    public int ReviewLevel { get; set; } = 1;

    public int CurrentLevel { get; set; } = 1;

    public YesOrNo Status { get; set; } = YesOrNo.Yes;
}

/// <summary>
/// 创建审查 DTO
/// </summary>
public class ReviewCreateDto : BasicAppCDto
{
    [Required(ErrorMessage = "审查编码不能为空")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "审查编码长度必须在 1～100 之间")]
    public string ReviewCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "审查标题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "审查标题长度必须在 1～200 之间")]
    public string ReviewTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "审查类型不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "审查类型长度必须在 1～50 之间")]
    public string ReviewType { get; set; } = string.Empty;

    public string? ReviewContent { get; set; }

    [StringLength(1000, ErrorMessage = "审查描述长度不能超过 1000")]
    public string? ReviewDescription { get; set; }

    [StringLength(100, ErrorMessage = "业务实体类型长度不能超过 100")]
    public string? EntityType { get; set; }

    [StringLength(100, ErrorMessage = "业务实体ID长度不能超过 100")]
    public string? EntityId { get; set; }

    public string? BusinessData { get; set; }

    [Range(1, 5, ErrorMessage = "优先级范围为 1～5")]
    public int Priority { get; set; } = 3;

    public long? SubmitUserId { get; set; }

    [StringLength(50, ErrorMessage = "提交人名称长度不能超过 50")]
    public string? SubmitUserName { get; set; }

    public DateTimeOffset SubmitTime { get; set; } = DateTimeOffset.UtcNow;

    public long? CurrentReviewUserId { get; set; }

    [StringLength(50, ErrorMessage = "当前审查人名称长度不能超过 50")]
    public string? CurrentReviewUserName { get; set; }

    public string? ReviewUserIds { get; set; }

    public int ReviewLevel { get; set; } = 1;

    public int CurrentLevel { get; set; } = 1;

    public long? TenantId { get; set; }

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新审查 DTO
/// </summary>
public class ReviewUpdateDto : BasicAppUDto
{
    [Required(ErrorMessage = "审查标题不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "审查标题长度必须在 1～200 之间")]
    public string ReviewTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "审查类型不能为空")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "审查类型长度必须在 1～50 之间")]
    public string ReviewType { get; set; } = string.Empty;

    public string? ReviewContent { get; set; }

    [StringLength(1000, ErrorMessage = "审查描述长度不能超过 1000")]
    public string? ReviewDescription { get; set; }

    [StringLength(100, ErrorMessage = "业务实体类型长度不能超过 100")]
    public string? EntityType { get; set; }

    [StringLength(100, ErrorMessage = "业务实体ID长度不能超过 100")]
    public string? EntityId { get; set; }

    public string? BusinessData { get; set; }

    public AuditStatus ReviewStatus { get; set; } = AuditStatus.Pending;

    public AuditResult? ReviewResult { get; set; }

    [Range(1, 5, ErrorMessage = "优先级范围为 1～5")]
    public int Priority { get; set; } = 3;

    public long? CurrentReviewUserId { get; set; }

    [StringLength(50, ErrorMessage = "当前审查人名称长度不能超过 50")]
    public string? CurrentReviewUserName { get; set; }

    public string? ReviewUserIds { get; set; }

    public int ReviewLevel { get; set; } = 1;

    public int CurrentLevel { get; set; } = 1;

    [StringLength(1000, ErrorMessage = "审查意见长度不能超过 1000")]
    public string? ReviewComment { get; set; }

    public DateTimeOffset? ReviewStartTime { get; set; }

    public DateTimeOffset? ReviewEndTime { get; set; }

    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    [StringLength(500, ErrorMessage = "备注长度不能超过 500")]
    public string? Remark { get; set; }
}
