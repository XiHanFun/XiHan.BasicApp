#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysReviewDto
// Guid:3628152c-d6e9-4396-addb-b479254bad99
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统审查查询 DTO
/// </summary>
public class SysReviewGetDto : RbacDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 审查描述
    /// </summary>
    public string? ReviewDescription { get; set; }

    /// <summary>
    /// 业务实体类型
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 业务实体ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// 业务数据
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
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 提交人ID
    /// </summary>
    public long? SubmitterId { get; set; }

    /// <summary>
    /// 提交人名称
    /// </summary>
    public string? SubmitterName { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    public DateTimeOffset SubmitTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 当前审查人ID
    /// </summary>
    public long? CurrentReviewerId { get; set; }

    /// <summary>
    /// 当前审查人名称
    /// </summary>
    public string? CurrentReviewerName { get; set; }

    /// <summary>
    /// 审查人ID列表
    /// </summary>
    public string? ReviewerIds { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    public int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 当前审查级别
    /// </summary>
    public int CurrentLevel { get; set; } = 1;

    /// <summary>
    /// 审查意见
    /// </summary>
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
    /// 审查耗时（秒）
    /// </summary>
    public long ReviewDuration { get; set; } = 0;

    /// <summary>
    /// 附件信息
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改者ID
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 系统审查创建 DTO
/// </summary>
public class SysReviewCreateDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

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
    /// 审查描述
    /// </summary>
    public string? ReviewDescription { get; set; }

    /// <summary>
    /// 业务实体类型
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 业务实体ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// 业务数据
    /// </summary>
    public string? BusinessData { get; set; }

    /// <summary>
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 审查人ID列表
    /// </summary>
    public string? ReviewerIds { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    public int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 附件信息
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统审查更新 DTO
/// </summary>
public class SysReviewUpdateDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 审查标题
    /// </summary>
    public string? ReviewTitle { get; set; }

    /// <summary>
    /// 审查内容
    /// </summary>
    public string? ReviewContent { get; set; }

    /// <summary>
    /// 审查描述
    /// </summary>
    public string? ReviewDescription { get; set; }

    /// <summary>
    /// 业务数据
    /// </summary>
    public string? BusinessData { get; set; }

    /// <summary>
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// 附件信息
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 系统审查处理 DTO
/// </summary>
public class SysReviewProcessDto
{
    /// <summary>
    /// 审查ID
    /// </summary>
    public long ReviewId { get; set; }

    /// <summary>
    /// 审查结果
    /// </summary>
    public AuditResult ReviewResult { get; set; }

    /// <summary>
    /// 审查意见
    /// </summary>
    public string? ReviewComment { get; set; }

    /// <summary>
    /// 附件信息
    /// </summary>
    public string? Attachments { get; set; }
}
