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

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统用户会话实体
/// </summary>
[SugarTable("Sys_User_Session", "系统用户会话表")]
[SugarIndex("IX_SysUserSession_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysUserSession_To", nameof(Token), OrderByType.Asc, true)]
[SugarIndex("IX_SysUserSession_ReTo", nameof(RefreshToken), OrderByType.Asc)]
[SugarIndex("IX_SysUserSession_SeId", nameof(SessionId), OrderByType.Asc)]
[SugarIndex("IX_SysUserSession_ToExAt", nameof(TokenExpiresAt), OrderByType.Asc)]
[SugarIndex("IX_SysUserSession_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
public partial class SysUserSession : BasicAppAggregateRoot
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 访问令牌
    /// </summary>
    [SugarColumn(ColumnDescription = "访问令牌", Length = 1000, IsNullable = false)]
    public virtual string Token { get; set; } = string.Empty;

    /// <summary>
    /// 刷新令牌
    /// </summary>
    [SugarColumn(ColumnDescription = "刷新令牌", Length = 1000, IsNullable = true)]
    public virtual string? RefreshToken { get; set; }

    /// <summary>
    /// 会话标识（用于区分不同设备）
    /// </summary>
    [SugarColumn(ColumnDescription = "会话标识", Length = 100, IsNullable = false)]
    public virtual string SessionId { get; set; } = string.Empty;

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
    /// Token过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "Token过期时间")]
    public virtual DateTimeOffset TokenExpiresAt { get; set; }

    /// <summary>
    /// RefreshToken过期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "RefreshToken过期时间", IsNullable = true)]
    public virtual DateTimeOffset? RefreshTokenExpiresAt { get; set; }

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
