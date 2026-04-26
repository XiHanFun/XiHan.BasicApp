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
using XiHan.BasicApp.Saas.Domain.Entities.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户会话实体（会话中心）
/// 职责：登录状态、多端控制、在线状态、会话撤销、设备管理。
/// 不存储完整 Token/RefreshToken，仅通过 AccessTokenJti 与 Token 表关联，便于黑名单与撤销。
/// </summary>
[SugarTable("SysUserSession", "系统用户会话表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsSeId", nameof(TenantId), OrderByType.Asc, nameof(UserSessionId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_AcJti", nameof(CurrentAccessTokenJti), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExAt", nameof(ExpiresAt), OrderByType.Asc)]
public partial class SysUserSession : BasicAppFullAuditedEntity
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
    /// 会话标识（用于区分不同设备/端，在同一租户上下文内唯一）
    /// 同一自然人在不同租户同时登录时允许复用同一业务 SessionId，但会落成不同 TenantId 的独立会话记录
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
    public virtual DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后活动时间")]
    public virtual DateTimeOffset LastActivityTime { get; set; }

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
    /// 会话过期时间（绝对超时，如 24 小时强制重登；为空表示不限）
    /// </summary>
    [SugarColumn(ColumnDescription = "会话过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
