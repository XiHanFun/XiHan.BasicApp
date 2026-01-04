#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditDto
// Guid:e1f2g3h4-i5j6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 16:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Audits.Dtos;

/// <summary>
/// 审核 DTO
/// </summary>
public class AuditDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 审核标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 审核内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 业务ID
    /// </summary>
    public long BusinessId { get; set; }

    /// <summary>
    /// 业务数据（JSON格式）
    /// </summary>
    public string? BusinessData { get; set; }

    /// <summary>
    /// 提交用户ID
    /// </summary>
    public long SubmitterId { get; set; }

    /// <summary>
    /// 审核用户ID
    /// </summary>
    public long? AuditorId { get; set; }

    /// <summary>
    /// 审核状态
    /// </summary>
    public AuditStatus AuditStatus { get; set; } = AuditStatus.Pending;

    /// <summary>
    /// 审核结果
    /// </summary>
    public AuditResult? AuditResult { get; set; }

    /// <summary>
    /// 审核意见
    /// </summary>
    public string? AuditOpinion { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    public DateTimeOffset SubmitTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 审核时间
    /// </summary>
    public DateTimeOffset? AuditTime { get; set; }

    /// <summary>
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 是否需要多级审核
    /// </summary>
    public bool IsMultiLevel { get; set; } = false;

    /// <summary>
    /// 当前审核级别
    /// </summary>
    public int CurrentLevel { get; set; } = 1;

    /// <summary>
    /// 总审核级别
    /// </summary>
    public int TotalLevel { get; set; } = 1;

    /// <summary>
    /// 截止时间
    /// </summary>
    public DateTimeOffset? Deadline { get; set; }

    /// <summary>
    /// 附件路径（多个用逗号分隔）
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建审核 DTO
/// </summary>
public class CreateAuditDto : RbacCreationDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 审核标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 审核内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    public string BusinessType { get; set; } = string.Empty;

    /// <summary>
    /// 业务ID
    /// </summary>
    public long BusinessId { get; set; }

    /// <summary>
    /// 业务数据（JSON格式）
    /// </summary>
    public string? BusinessData { get; set; }

    /// <summary>
    /// 提交用户ID
    /// </summary>
    public long SubmitterId { get; set; }

    /// <summary>
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 是否需要多级审核
    /// </summary>
    public bool IsMultiLevel { get; set; } = false;

    /// <summary>
    /// 总审核级别
    /// </summary>
    public int TotalLevel { get; set; } = 1;

    /// <summary>
    /// 截止时间
    /// </summary>
    public DateTimeOffset? Deadline { get; set; }

    /// <summary>
    /// 附件路径（多个用逗号分隔）
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新审核 DTO
/// </summary>
public class UpdateAuditDto : RbacUpdateDtoBase
{
    /// <summary>
    /// 审核标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 审核内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 业务数据（JSON格式）
    /// </summary>
    public string? BusinessData { get; set; }

    /// <summary>
    /// 优先级（1-5，数字越小优先级越高）
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// 截止时间
    /// </summary>
    public DateTimeOffset? Deadline { get; set; }

    /// <summary>
    /// 附件路径（多个用逗号分隔）
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 审核处理 DTO
/// </summary>
public class ProcessAuditDto
{
    /// <summary>
    /// 审核ID
    /// </summary>
    public long AuditId { get; set; }

    /// <summary>
    /// 审核用户ID
    /// </summary>
    public long AuditorId { get; set; }

    /// <summary>
    /// 审核结果
    /// </summary>
    public AuditResult AuditResult { get; set; }

    /// <summary>
    /// 审核意见
    /// </summary>
    public string? AuditOpinion { get; set; }
}
