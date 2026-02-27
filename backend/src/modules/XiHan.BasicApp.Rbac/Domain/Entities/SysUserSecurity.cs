#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSecurity
// Guid:1d28152c-d6e9-4396-addb-b479254bad14
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 19:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

/// <summary>
/// 系统用户安全状态实体
/// </summary>
[SugarTable("Sys_User_Security", "系统用户安全状态表")]
[SugarIndex("UX_SysUserSecurity_UsId", nameof(UserId), OrderByType.Asc, true)]
[SugarIndex("IX_SysUserSecurity_LoEnTi", nameof(LockoutEndTime), OrderByType.Asc)]
public partial class SysUserSecurity : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 最后修改密码时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后修改密码时间", IsNullable = true)]
    public virtual DateTimeOffset? LastPasswordChangeTime { get; set; }

    /// <summary>
    /// 密码过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "密码过期时间", IsNullable = true)]
    public virtual DateTimeOffset? PasswordExpiryTime { get; set; }

    /// <summary>
    /// 失败登录次数
    /// </summary>
    [SugarColumn(ColumnDescription = "失败登录次数")]
    public virtual int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// 最后失败登录时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后失败登录时间", IsNullable = true)]
    public virtual DateTimeOffset? LastFailedLoginTime { get; set; }

    /// <summary>
    /// 是否锁定
    /// </summary>
    [SugarColumn(ColumnDescription = "是否锁定")]
    public virtual bool IsLocked { get; set; } = false;

    /// <summary>
    /// 锁定时间
    /// </summary>
    [SugarColumn(ColumnDescription = "锁定时间", IsNullable = true)]
    public virtual DateTimeOffset? LockoutTime { get; set; }

    /// <summary>
    /// 锁定结束时间
    /// </summary>
    [SugarColumn(ColumnDescription = "锁定结束时间", IsNullable = true)]
    public virtual DateTimeOffset? LockoutEndTime { get; set; }

    /// <summary>
    /// 是否启用双因素认证
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用双因素认证")]
    public virtual bool TwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// 双因素认证密钥（敏感信息）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "双因素认证密钥", Length = 200, IsNullable = true)]
    public virtual string? TwoFactorSecret { get; set; }

    /// <summary>
    /// 安全戳（用于强制重新登录）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "安全戳", Length = 100, IsNullable = true)]
    public virtual string? SecurityStamp { get; set; }

    /// <summary>
    /// 邮箱是否验证
    /// </summary>
    [SugarColumn(ColumnDescription = "邮箱是否验证")]
    public virtual bool EmailVerified { get; set; } = false;

    /// <summary>
    /// 手机号是否验证
    /// </summary>
    [SugarColumn(ColumnDescription = "手机号是否验证")]
    public virtual bool PhoneVerified { get; set; } = false;

    /// <summary>
    /// 是否允许多端登录
    /// </summary>
    [SugarColumn(ColumnDescription = "是否允许多端登录")]
    public virtual bool AllowMultiLogin { get; set; } = true;

    /// <summary>
    /// 最大登录设备数（0表示不限制）
    /// </summary>
    [SugarColumn(ColumnDescription = "最大登录设备数")]
    public virtual int MaxLoginDevices { get; set; } = 0;

    /// <summary>
    /// 上次安全检查时间
    /// </summary>
    [SugarColumn(ColumnDescription = "上次安全检查时间", IsNullable = true)]
    public virtual DateTimeOffset? LastSecurityCheckTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
