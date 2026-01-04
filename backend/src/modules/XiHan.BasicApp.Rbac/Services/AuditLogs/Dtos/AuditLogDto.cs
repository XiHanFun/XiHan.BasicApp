#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditLogDto
// Guid:d1c2d3e4-f5a6-7890-abcd-ef1234567894
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.AuditLogs.Dtos;

/// <summary>
/// 审核日志 DTO
/// </summary>
public class AuditLogDto : RbacFullAuditedDtoBase
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
    /// 审核级别
    /// </summary>
    public int AuditLevel { get; set; } = 1;

    /// <summary>
    /// 审核结果
    /// </summary>
    public AuditResult AuditResult { get; set; } = AuditResult.Pass;

    /// <summary>
    /// 审核意见
    /// </summary>
    public string? AuditOpinion { get; set; }

    /// <summary>
    /// 审核时间
    /// </summary>
    public DateTimeOffset AuditTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 审核前状态
    /// </summary>
    public AuditStatus BeforeStatus { get; set; } = AuditStatus.Pending;

    /// <summary>
    /// 审核后状态
    /// </summary>
    public AuditStatus AfterStatus { get; set; } = AuditStatus.Approved;

    /// <summary>
    /// 审核IP
    /// </summary>
    public string? AuditIp { get; set; }

    /// <summary>
    /// 审核位置
    /// </summary>
    public string? AuditLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 附件路径（多个用逗号分隔）
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
