#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysReviewLogDto
// Guid:4628152c-d6e9-4396-addb-b479254bada0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统审查日志查询 DTO
/// </summary>
public class SysReviewLogGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 审查ID
    /// </summary>
    public long ReviewId { get; set; }

    /// <summary>
    /// 审查编码
    /// </summary>
    public string? ReviewCode { get; set; }

    /// <summary>
    /// 审查标题
    /// </summary>
    public string? ReviewTitle { get; set; }

    /// <summary>
    /// 审查类型
    /// </summary>
    public string? ReviewType { get; set; }

    /// <summary>
    /// 审查级别
    /// </summary>
    public int ReviewLevel { get; set; } = 1;

    /// <summary>
    /// 审查人ID
    /// </summary>
    public long? ReviewerId { get; set; }

    /// <summary>
    /// 审查人名称
    /// </summary>
    public string? ReviewerName { get; set; }

    /// <summary>
    /// 审查人部门
    /// </summary>
    public string? ReviewerDepartment { get; set; }

    /// <summary>
    /// 原审查状态
    /// </summary>
    public AuditStatus OriginalStatus { get; set; }

    /// <summary>
    /// 新审查状态
    /// </summary>
    public AuditStatus NewStatus { get; set; }

    /// <summary>
    /// 审查结果
    /// </summary>
    public AuditResult ReviewResult { get; set; }

    /// <summary>
    /// 审查意见
    /// </summary>
    public string? ReviewComment { get; set; }

    /// <summary>
    /// 审查动作
    /// </summary>
    public string? ReviewAction { get; set; }

    /// <summary>
    /// 审查前数据
    /// </summary>
    public string? BeforeData { get; set; }

    /// <summary>
    /// 审查后数据
    /// </summary>
    public string? AfterData { get; set; }

    /// <summary>
    /// 数据变更内容
    /// </summary>
    public string? ChangeContent { get; set; }

    /// <summary>
    /// 附件信息
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// 审查IP
    /// </summary>
    public string? ReviewIp { get; set; }

    /// <summary>
    /// 审查地址
    /// </summary>
    public string? ReviewLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public string? Device { get; set; }

    /// <summary>
    /// 审查时间
    /// </summary>
    public DateTimeOffset ReviewTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 审查耗时（毫秒）
    /// </summary>
    public long ReviewDuration { get; set; } = 0;

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
