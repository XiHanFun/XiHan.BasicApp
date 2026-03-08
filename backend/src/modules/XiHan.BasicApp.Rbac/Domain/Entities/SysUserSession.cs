#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserSession
// Guid:2d28152c-d6e9-4396-addb-b479254bad15
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 20:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

/// <summary>
/// 系统用户会话实体（会话中心）
/// 职责：登录状态、多端控制、在线状态、会话撤销、设备管理。
/// 不存储完整 Token/RefreshToken，仅通过 AccessTokenJti 与 Token 表关联，便于黑名单与撤销。
/// </summary>
[SugarTable("Sys_User_Session", "系统用户会话表")]
[SugarIndex("IX_SysUserSession_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysUserSession_UsSeId", nameof(UserSessionId), OrderByType.Asc, true)]
[SugarIndex("IX_SysUserSession_AcJti", nameof(CurrentAccessTokenJti), OrderByType.Asc)]
[SugarIndex("IX_SysUserSession_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysUserSession_IsOn_IsRe", nameof(IsOnline), OrderByType.Asc, nameof(IsRevoked), OrderByType.Asc)]
public partial class SysUserSession : BasicAppAggregateRoot
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 当前访问令牌 JTI（JWT ID，用于黑名单/撤销校验，不存完整 Token）
    /// </summary>
    [SugarColumn(ColumnDescription = "当前访问令牌JTI", Length = 200, IsNullable = true)]
    public virtual string? CurrentAccessTokenJti { get; set; }

    /// <summary>
    /// 会话标识（用于区分不同设备/端，业务侧唯一）
    /// </summary>
    [SugarColumn(ColumnDescription = "会话标识", Length = 100, IsNullable = false)]
    public virtual string UserSessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    [SugarColumn(ColumnDescription = "设备类型")]
    public virtual DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备名称
    /// </summary>
    [SugarColumn(ColumnDescription = "设备名称", Length = 200, IsNullable = true)]
    public virtual string? DeviceName { get; set; }

    /// <summary>
    /// 设备ID（设备唯一标识）
    /// </summary>
    [SugarColumn(ColumnDescription = "设备ID", Length = 200, IsNullable = true)]
    public virtual string? DeviceId { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "操作系统", Length = 100, IsNullable = true)]
    public virtual string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    [SugarColumn(ColumnDescription = "浏览器", Length = 100, IsNullable = true)]
    public virtual string? Browser { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [SugarColumn(ColumnDescription = "IP地址", Length = 50, IsNullable = true)]
    public virtual string? IpAddress { get; set; }

    /// <summary>
    /// 登录位置
    /// </summary>
    [SugarColumn(ColumnDescription = "登录位置", Length = 200, IsNullable = true)]
    public virtual string? Location { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    [SugarColumn(ColumnDescription = "登录时间")]
    public virtual DateTimeOffset LoginTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 最后活动时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后活动时间")]
    public virtual DateTimeOffset LastActivityTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 是否在线
    /// </summary>
    [SugarColumn(ColumnDescription = "是否在线")]
    public virtual bool IsOnline { get; set; } = true;

    /// <summary>
    /// 是否已撤销
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已撤销")]
    public virtual bool IsRevoked { get; set; } = false;

    /// <summary>
    /// 撤销时间
    /// </summary>
    [SugarColumn(ColumnDescription = "撤销时间", IsNullable = true)]
    public virtual DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    [SugarColumn(ColumnDescription = "撤销原因", Length = 200, IsNullable = true)]
    public virtual string? RevokedReason { get; set; }

    /// <summary>
    /// 登出时间
    /// </summary>
    [SugarColumn(ColumnDescription = "登出时间", IsNullable = true)]
    public virtual DateTimeOffset? LogoutTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
