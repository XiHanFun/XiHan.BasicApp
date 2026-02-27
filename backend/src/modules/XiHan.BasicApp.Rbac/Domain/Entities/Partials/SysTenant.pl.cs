#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenant.pl
// Guid:2d28152c-d6e9-4396-addb-b479254bad36
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:51:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统租户实体扩展
/// </summary>
public partial class SysTenant
{
    /// <summary>
    /// 租户配置列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysConfig.TenantId))]
    public virtual List<SysConfig>? Configs { get; set; }

    /// <summary>
    /// 租户用户列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUser.BasicId))]
    public virtual List<SysUser>? Users { get; set; }

    /// <summary>
    /// 租户文件列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysFile.TenantId))]
    public virtual List<SysFile>? Files { get; set; }

    /// <summary>
    /// 租户通知列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysNotification.TenantId))]
    public virtual List<SysNotification>? Notifications { get; set; }

    /// <summary>
    /// 租户操作日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysOperationLog.TenantId))]
    public virtual List<SysOperationLog>? OperationLogs { get; set; }

    /// <summary>
    /// 租户邮件列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysEmail.TenantId))]
    public virtual List<SysEmail>? Emails { get; set; }

    /// <summary>
    /// 租户短信列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysSms.TenantId))]
    public virtual List<SysSms>? SmsMessages { get; set; }

    /// <summary>
    /// 租户统计列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserStatistics.TenantId))]
    public virtual List<SysUserStatistics>? UserStatistics { get; set; }

    /// <summary>
    /// 租户审查列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysReview.TenantId))]
    public virtual List<SysReview>? Reviews { get; set; }

    /// <summary>
    /// 租户审查日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysReviewLog.TenantId))]
    public virtual List<SysReviewLog>? ReviewLogs { get; set; }

    /// <summary>
    /// 租户审计日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysAuditLog.TenantId))]
    public virtual List<SysAuditLog>? AuditLogs { get; set; }

    /// <summary>
    /// 租户访问日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysAccessLog.TenantId))]
    public virtual List<SysAccessLog>? AccessLogs { get; set; }

    /// <summary>
    /// 租户任务列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysTask.TenantId))]
    public virtual List<SysTask>? Tasks { get; set; }

    /// <summary>
    /// 租户API日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysApiLog.TenantId))]
    public virtual List<SysApiLog>? ApiLogs { get; set; }
}
