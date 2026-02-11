#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUser.pl
// Guid:bc28152c-d6e9-4396-addb-b479254bad15
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 03:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统用户实体扩展
/// </summary>
public partial class SysUser
{
    /// <summary>
    /// 用户角色关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserRole.UserId))]
    public virtual List<SysUserRole>? UserRoles { get; set; }

    /// <summary>
    /// 用户部门关联列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserDepartment.UserId))]
    public virtual List<SysUserDepartment>? UserDepartments { get; set; }

    /// <summary>
    /// 角色列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(typeof(SysUserRole), nameof(SysUserRole.UserId), nameof(SysUserRole.RoleId))]
    public virtual List<SysRole>? Roles { get; set; }

    /// <summary>
    /// 部门列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(typeof(SysUserDepartment), nameof(SysUserDepartment.UserId), nameof(SysUserDepartment.DepartmentId))]
    public virtual List<SysDepartment>? Departments { get; set; }

    /// <summary>
    /// 登录日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysLoginLog.UserId))]
    public virtual List<SysLoginLog>? LoginLogs { get; set; }

    /// <summary>
    /// 上传文件列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysFile.CreatedId))]
    public virtual List<SysFile>? UploadedFiles { get; set; }

    /// <summary>
    /// 操作日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysOperationLog.UserId))]
    public virtual List<SysOperationLog>? OperationLogs { get; set; }

    /// <summary>
    /// 接收通知列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysNotification.RecipientUserId))]
    public virtual List<SysNotification>? ReceivedNotifications { get; set; }

    /// <summary>
    /// 发送通知列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysNotification.SendUserId))]
    public virtual List<SysNotification>? SentNotifications { get; set; }

    /// <summary>
    /// OAuth授权码列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysOAuthCode.UserId))]
    public virtual List<SysOAuthCode>? OAuthCodes { get; set; }

    /// <summary>
    /// OAuth令牌列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysOAuthToken.UserId))]
    public virtual List<SysOAuthToken>? OAuthTokens { get; set; }

    /// <summary>
    /// 发送邮件列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysEmail.SendUserId))]
    public virtual List<SysEmail>? SentEmails { get; set; }

    /// <summary>
    /// 接收邮件列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysEmail.ReceiveUserId))]
    public virtual List<SysEmail>? ReceivedEmails { get; set; }

    /// <summary>
    /// 发送短信列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysSms.SenderId))]
    public virtual List<SysSms>? SentSms { get; set; }

    /// <summary>
    /// 接收短信列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysSms.ReceiverId))]
    public virtual List<SysSms>? ReceivedSms { get; set; }

    /// <summary>
    /// 用户统计列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysUserStatistics.UserId))]
    public virtual List<SysUserStatistics>? UserStatistics { get; set; }

    /// <summary>
    /// 提交审查列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysReview.SubmitUserId))]
    public virtual List<SysReview>? SubmittedReviews { get; set; }

    /// <summary>
    /// 当前审查列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysReview.CurrentReviewUserId))]
    public virtual List<SysReview>? CurrentReviews { get; set; }

    /// <summary>
    /// 审查日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysReviewLog.ReviewUserId))]
    public virtual List<SysReviewLog>? ReviewLogs { get; set; }

    /// <summary>
    /// 审计日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysAuditLog.UserId))]
    public virtual List<SysAuditLog>? AuditLogs { get; set; }

    /// <summary>
    /// 访问日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysAccessLog.UserId))]
    public virtual List<SysAccessLog>? AccessLogs { get; set; }

    /// <summary>
    /// API日志列表
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(SysApiLog.UserId))]
    public virtual List<SysApiLog>? ApiLogs { get; set; }
}
