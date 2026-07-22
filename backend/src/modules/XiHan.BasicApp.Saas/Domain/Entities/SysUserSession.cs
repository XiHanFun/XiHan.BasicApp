// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户会话实体（会话中心）
/// 职责：登录状态、多端控制、在线状态、会话撤销、设备管理。
/// 不存储完整 Token/RefreshToken，仅通过 AccessTokenJti 与 Token 表关联，便于黑名单与撤销。
/// </summary>
[SugarTable(TableName = "Sys_User_Session", TableDescription = "系统用户会话表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsSeId", nameof(TenantId), OrderByType.Asc, nameof(UserSessionId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_AcJti", nameof(CurrentAccessTokenJti), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_UsId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExTi", nameof(ExpirationTime), OrderByType.Desc)]
public partial class SysUserSession : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnName = "User_Id", ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 当前访问令牌 JTI（JWT ID，用于黑名单/撤销校验，不存完整 Token）
    /// </summary>
    [SugarColumn(ColumnName = "Current_Access_Token_Jti", ColumnDescription = "当前访问令牌JTI", Length = 200, IsNullable = true)]
    public virtual string? CurrentAccessTokenJti { get; set; }

    /// <summary>
    /// 会话标识（用于区分不同设备/端，在同一租户上下文内唯一）
    /// 同一自然人在不同租户同时登录时允许复用同一业务 SessionId，但会落成不同 TenantId 的独立会话记录
    /// </summary>
    [SugarColumn(ColumnName = "User_Session_Id", ColumnDescription = "会话标识", Length = 100, IsNullable = false)]
    public virtual string UserSessionId { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    [SugarColumn(ColumnName = "Device_Type", ColumnDescription = "设备类型")]
    public virtual DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备名称
    /// </summary>
    [SugarColumn(ColumnName = "Device_Name", ColumnDescription = "设备名称", Length = 200, IsNullable = true)]
    public virtual string? DeviceName { get; set; }

    /// <summary>
    /// 设备ID（设备唯一标识）
    /// </summary>
    [SugarColumn(ColumnName = "Device_Id", ColumnDescription = "设备ID", Length = 200, IsNullable = true)]
    public virtual string? DeviceId { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnName = "Operating_System", ColumnDescription = "操作系统", Length = 100, IsNullable = true)]
    public virtual string? OperatingSystem { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    [SugarColumn(ColumnName = "Browser", ColumnDescription = "浏览器", Length = 100, IsNullable = true)]
    public virtual string? Browser { get; set; }

    /// <summary>
    /// IP地址
    /// </summary>
    [SugarColumn(ColumnName = "Ip_Address", ColumnDescription = "IP地址", Length = 50, IsNullable = true)]
    public virtual string? IpAddress { get; set; }

    /// <summary>
    /// 登录位置
    /// </summary>
    [SugarColumn(ColumnName = "Location", ColumnDescription = "登录位置", Length = 200, IsNullable = true)]
    public virtual string? Location { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    [SugarColumn(ColumnName = "Login_Time", ColumnDescription = "登录时间")]
    public virtual DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    [SugarColumn(ColumnName = "Last_Activity_Time", ColumnDescription = "最后活动时间")]
    public virtual DateTimeOffset LastActivityTime { get; set; }

    /// <summary>
    /// 会话状态（Active=活跃 / Offline=离线 / Revoked=已撤销 / Expired=已过期；统一替代原 IsOnline + IsRevoked 布尔）
    /// </summary>
    /// <remarks>
    /// 状态判定建议：
    /// - 新建登录会话：Active
    /// - 正常登出 / 心跳超时：Offline（配合 LogoutTime）
    /// - 强制下线 / 安全撤销：Revoked（配合 RevokedTime、RevokedReason）
    /// - 超过 ExpirationTime：Expired（定时任务扫描置位）
    /// 鉴权"会话有效"判定：Status == Active 且未软删。
    /// </remarks>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "会话状态")]
    public virtual SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>
    /// 是否锁定
    /// </summary>
    /// <remarks>
    /// 锁定是<b>服务端强制</b>的：置位后 <c>XiHanSessionStateMiddleware</c> 会以 <b>423</b> 拒绝该会话的一切请求
    /// （仅放行解锁 / 登出 / 刷新令牌），因此改 DOM、开新标签页、直接 curl 调 API 都绕不过去。
    /// <para>
    /// <b>锁定不等于锁屏</b>：锁屏只是<see cref="LockReason"/> 的一种取值。将来若加入风控挂起、
    /// 强制改密、二次验证待完成等场景，复用同一个锁定位即可——解锁方式由原因决定。
    /// </para>
    /// <para>
    /// 与 <see cref="Status"/> <b>正交</b>：锁定不改变会话有效性——所以是 423 而非 401，
    /// 客户端应引导解锁而不是跳登录；解锁后原会话继续可用。
    /// </para>
    /// <para>
    /// 刷新令牌会保留同一 <c>session_id</c>，故锁定位可安全跨令牌刷新存活（长时间锁定不会掉成登出）。
    /// </para>
    /// </remarks>
    [SugarColumn(ColumnName = "Is_Locked", ColumnDescription = "是否锁定")]
    public virtual bool IsLocked { get; set; } = false;

    /// <summary>
    /// 锁定原因（<see cref="SessionLockReasons"/>；决定客户端引导哪种解锁方式）
    /// </summary>
    [SugarColumn(ColumnName = "Lock_Reason", ColumnDescription = "锁定原因", Length = 50, IsNullable = true)]
    public virtual string? LockReason { get; set; }

    /// <summary>
    /// 锁定时间
    /// </summary>
    [SugarColumn(ColumnName = "Locked_Time", ColumnDescription = "锁定时间", IsNullable = true)]
    public virtual DateTimeOffset? LockedTime { get; set; }

    /// <summary>
    /// 锁屏口令哈希（PBKDF2，仅可校验不可还原）
    /// </summary>
    /// <remarks>
    /// <b>仅用于 <see cref="SessionLockReasons.ScreenLock"/> 这一种锁定原因</b>——其它原因的锁定（如风控挂起）
    /// 不走口令解锁，此列为空。
    /// <para>
    /// <b>会话级</b>口令：锁屏时现场设置，随本次锁屏生效，解锁即清除——它不是账号密码，也不跨会话复用。
    /// 服务端<b>拒绝空口令锁屏</b>：无口令的锁屏在服务端强制模式下毫无意义（任何持有该 token 的人调一次解锁接口就开了）。
    /// </para>
    /// 解锁失败次数由 <see cref="UnlockFailedAttempts"/> 单独计数，<b>不复用</b>账号的登录失败计数，避免锁屏误触发账号锁定。
    /// </remarks>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(ColumnName = "Lock_Password_Hash", ColumnDescription = "锁屏口令哈希", Length = 200, IsNullable = true)]
    public virtual string? LockPasswordHash { get; set; }

    /// <summary>
    /// 解锁失败次数
    /// </summary>
    [SugarColumn(ColumnName = "Unlock_Failed_Attempts", ColumnDescription = "解锁失败次数")]
    public virtual int UnlockFailedAttempts { get; set; } = 0;

    /// <summary>
    /// 撤销时间
    /// </summary>
    [SugarColumn(ColumnName = "Revoked_Time", ColumnDescription = "撤销时间", IsNullable = true)]
    public virtual DateTimeOffset? RevokedTime { get; set; }

    /// <summary>
    /// 撤销原因
    /// </summary>
    [SugarColumn(ColumnName = "Revoked_Reason", ColumnDescription = "撤销原因", Length = 200, IsNullable = true)]
    public virtual string? RevokedReason { get; set; }

    /// <summary>
    /// 登出时间
    /// </summary>
    [SugarColumn(ColumnName = "Logout_Time", ColumnDescription = "登出时间", IsNullable = true)]
    public virtual DateTimeOffset? LogoutTime { get; set; }

    /// <summary>
    /// 会话过期时间（绝对超时，如 24 小时强制重登；为空表示不限）
    /// </summary>
    [SugarColumn(ColumnName = "Expiration_Time", ColumnDescription = "会话过期时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
