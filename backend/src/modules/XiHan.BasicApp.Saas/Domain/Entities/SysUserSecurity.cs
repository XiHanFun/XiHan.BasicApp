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
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户安全状态实体
/// SysUser 的 1:1 安全扩展：承载锁定、MFA、失败计数等敏感安全字段，避免污染主表
/// </summary>
/// <remarks>
/// 职责边界：
/// - 与 SysUser 一对一（UX_UsId），独立表便于高频安全字段的访问控制与脱敏
/// - SysUser 负责"身份资料"，本表负责"安全状态"
/// - 设计决策：本表按用户唯一（不含 TenantId），表达统一身份的安全状态。
///   租户级安全策略（如"进入某租户必须 MFA"）应在 SysConfig 中按租户配置（ConfigKey=security.mfa.required），
///   登录时由服务层组合判断：用户安全状态（本表） + 目标租户安全策略（SysConfig）。
///
/// 关联：
/// - UserId → SysUser（一对一强约束）
///
/// 写入：
/// - UserId 唯一（UX_UsId）
/// - 创建 SysUser 时同步创建本表（默认未启用 MFA、失败次数=0）
/// - FailedLoginCount 在每次登录失败时 +1；成功登录时重置为 0
/// - 达到阈值时设置 LockoutEndTime，服务层判断是否允许登录
///
/// 查询：
/// - 登录前置校验：按 UserId 查安全状态
/// - 锁定中用户扫描：IX_LoEnTi + WHERE LockoutEndTime > now
///
/// 删除：
/// - 仅软删；随 SysUser 级联软删
///
/// 场景：
/// - 登录失败次数限制 / 账户临时锁定
/// - 多因素认证（MFA/TOTP）开关与密钥管理
/// - 密码最近修改时间、过期策略
/// </remarks>
[SugarTable("SysUserSecurity", "系统用户安全状态表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_UsId", nameof(UserId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_LoEnTi", nameof(LockoutEndTime), OrderByType.Asc)]
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
    /// 双因素认证方式
    /// </summary>
    [SugarColumn(ColumnDescription = "双因素认证方式")]
    public virtual TwoFactorMethod TwoFactorMethod { get; set; } = TwoFactorMethod.None;

    /// <summary>
    /// 双因素认证密钥（敏感信息，仅 TOTP 使用）
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnDescription = "双因素认证密钥", Length = 200, IsNullable = true)]
    public virtual string? TwoFactorSecret { get; set; }

    /// <summary>
    /// 最后修改用户名时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后修改用户名时间", IsNullable = true)]
    public virtual DateTimeOffset? LastUserNameChangeTime { get; set; }

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
