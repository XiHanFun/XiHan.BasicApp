// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 登录会话
/// </summary>
/// <remarks>
/// 一次登录一行，会话 ID 就是访问令牌的 <c>sid</c>：访问令牌无状态，要在过期前让它失效就得有一行可吊销的会话；会话门禁在 Authenticate 阶段按 <c>sid</c> 核对
/// 刷新令牌不透明、只存 SHA-256 散列，拿到库也换不出能用的令牌；续签、切换租户、激活角色都轮换它，旧的立即失效
/// 落平台库、不分租户：一个会话能在注册地、成员租户与平台之间切换（同一 <c>sid</c>），<see cref="ActiveTenantId"/> 记它当前所在的一侧（平台为 <see langword="null"/>），在线用户按这一列分侧
/// 门禁每个请求都要核对，判定进共享缓存、吊销与锁屏解锁时主动失效；共享缓存是进程内实现时，其他实例最迟在 <c>BasicApp:Auth:SessionCacheSeconds</c> 后生效。经账号域服务的吊销还在提交后推送强制下线并断开实时连接
/// 吊销只置 <see cref="RevokedAt"/>、不删行：个人中心的在线时长按会话起止计算，下线原因也留着；代价是没有清理作业，行会一直累积
/// 模仿登录另开一行，记发起人与他当时的会话：发起人的会话保留，结束模仿时回到它；它失效（注销、被吊销或过期）时模仿会话随之失效。模仿会话的期限在发起时定死，续签不顺延
/// 锁定记在会话上而不是账号上：锁屏只作用于这一个端，不影响同一用户的其他端；强制改密锁也按会话，解开它的办法就是改密。解锁的失败计数与吊销在独立作用域里提交，否则随失败的调用回滚
/// 每次续签都写，属写得极频繁的运行态数据，标 <c>[DiffLogIgnored]</c> 不进数据变更日志
/// 索引按读法建：刷新令牌散列唯一（部分唯一索引）、按用户找有效会话、按一侧列在线用户、按发起人与原会话找模仿会话
/// </remarks>
[SugarTable(TableName = "sys_user_session", TableDescription = "登录会话")]
[ActiveUniqueIndex(nameof(RefreshTokenHash), Name = "ux_sys_user_session_refresh_token_hash")]
[SugarIndex("ix_sys_user_session_user_id_revoked_at", nameof(UserId), OrderByType.Asc, nameof(RevokedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_user_session_active_tenant_id_revoked_at_issued_at", nameof(ActiveTenantId), OrderByType.Asc, nameof(RevokedAt), OrderByType.Asc, nameof(IssuedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_user_session_impersonator_session_id", nameof(ImpersonatorSessionId), OrderByType.Asc)]
[SugarIndex("ix_sys_user_session_impersonator_user_id", nameof(ImpersonatorUserId), OrderByType.Asc)]
[DiffLogIgnored]
public sealed class SysUserSession : AppAuditedEntity
{
    /// <summary>
    /// 用户 ID
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户 ID")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 本会话的当前租户
    /// </summary>
    /// <remarks>
    /// 平台账号为 <see langword="null"/>
    /// </remarks>
    [SugarColumn(ColumnName = "active_tenant_id", Length = 512, IsNullable = true, ColumnDescription = "本会话的当前租户")]
    public string? ActiveTenantId { get; set; }

    /// <summary>
    /// 刷新令牌的 SHA-256 散列（十六进制）
    /// </summary>
    [SugarColumn(ColumnName = "refresh_token_hash", Length = 512, IsNullable = false, ColumnDescription = "刷新令牌的 SHA-256 散列（十六进制）")]
    public string RefreshTokenHash { get; set; } = string.Empty;

    /// <summary>
    /// 签发时间
    /// </summary>
    [SugarColumn(ColumnName = "issued_at", IsNullable = false, ColumnDescription = "签发时间")]
    public DateTimeOffset IssuedAt { get; set; }

    /// <summary>
    /// 刷新令牌（会话）过期时间
    /// </summary>
    /// <remarks>
    /// 之后必须重新登录
    /// </remarks>
    [SugarColumn(ColumnName = "refresh_expires_at", IsNullable = false, ColumnDescription = "刷新令牌（会话）过期时间")]
    public DateTimeOffset RefreshExpiresAt { get; set; }

    /// <summary>
    /// 最近一次刷新时间
    /// </summary>
    [SugarColumn(ColumnName = "last_seen_at", IsNullable = true, ColumnDescription = "最近一次刷新时间")]
    public DateTimeOffset? LastSeenAt { get; set; }

    /// <summary>
    /// 吊销时间
    /// </summary>
    /// <remarks>
    /// 为 <see langword="null"/> 即有效
    /// </remarks>
    [SugarColumn(ColumnName = "revoked_at", IsNullable = true, ColumnDescription = "吊销时间")]
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// 吊销原因
    /// </summary>
    /// <remarks>
    /// 本人：退出 <c>logout</c>、改密 <c>password-changed</c>、找回密码 <c>password-reset</c>、吊销自己的会话 <c>self-revoked</c>、停用与注销账号 <c>self-deactivated</c> / <c>self-deleted</c>；模仿：结束模仿 <c>impersonation-stopped</c>、模仿方退出 <c>impersonator-logout</c>；管理员：吊销 <c>revoked</c>、停用 <c>disabled</c>、锁定 <c>locked</c>、删除 <c>deleted</c>；登录策略挤掉 <c>login-policy</c>；锁屏：没有锁屏密码 <c>lock-password-missing</c>、解锁失败次数用完 <c>unlock-exhausted</c>
    /// </remarks>
    [SugarColumn(ColumnName = "revoke_reason", Length = 512, IsNullable = true, ColumnDescription = "吊销原因")]
    public string? RevokeReason { get; set; }

    /// <summary>
    /// 客户端信息（User-Agent 等）
    /// </summary>
    [SugarColumn(ColumnName = "client_info", Length = 512, IsNullable = true, ColumnDescription = "客户端信息（User-Agent 等）")]
    public string? ClientInfo { get; set; }

    /// <summary>
    /// 登录 IP
    /// </summary>
    [SugarColumn(ColumnName = "ip_address", Length = 512, IsNullable = true, ColumnDescription = "登录 IP")]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 浏览器（开会话时由 User-Agent 粗分，在线用户页的展示与筛选列）
    /// </summary>
    [SugarColumn(ColumnName = "browser", Length = 64, IsNullable = true, ColumnDescription = "浏览器（开会话时由 User-Agent 粗分，在线用户页的展示与筛选列）")]
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统（同上）
    /// </summary>
    [SugarColumn(ColumnName = "os", Length = 64, IsNullable = true, ColumnDescription = "操作系统（同上）")]
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型：Desktop、Mobile、Tablet、Bot（同上）
    /// </summary>
    [SugarColumn(ColumnName = "device", Length = 32, IsNullable = true, ColumnDescription = "设备类型：Desktop、Mobile、Tablet、Bot（同上）")]
    public string? Device { get; set; }

    /// <summary>
    /// 登录地点
    /// </summary>
    /// <remarks>
    /// 开会话时按 IP 归属地解析。没装 IP 归属地库即为 <see langword="null"/>
    /// </remarks>
    [SugarColumn(ColumnName = "location", Length = 128, IsNullable = true, ColumnDescription = "登录地点")]
    public string? Location { get; set; }

    /// <summary>
    /// 模仿会话中发起模仿的用户 ID
    /// </summary>
    /// <remarks>
    /// 令牌的 <c>act</c>。普通会话为 <see langword="null"/>
    /// </remarks>
    [SugarColumn(ColumnName = "impersonator_user_id", IsNullable = true, ColumnDescription = "模仿会话中发起模仿的用户 ID")]
    public Guid? ImpersonatorUserId { get; set; }

    /// <summary>
    /// 模仿会话中发起人当时的会话
    /// </summary>
    /// <remarks>
    /// 结束模仿时回到它，它失效时模仿会话也失效
    /// </remarks>
    [SugarColumn(ColumnName = "impersonator_session_id", IsNullable = true, ColumnDescription = "模仿会话中发起人当时的会话")]
    public Guid? ImpersonatorSessionId { get; set; }

    /// <summary>
    /// 锁定原因
    /// </summary>
    /// <remarks>
    /// 为空即没锁，取值见 <see cref="SessionLockReasons"/>。锁定的会话只放行解锁与注销，强制改密锁另放行改密
    /// </remarks>
    [SugarColumn(ColumnName = "lock_reason", Length = 32, IsNullable = true, ColumnDescription = "锁定原因")]
    public string? LockReason { get; set; }

    /// <summary>
    /// 锁定时间
    /// </summary>
    [SugarColumn(ColumnName = "locked_at", IsNullable = true, ColumnDescription = "锁定时间")]
    public DateTimeOffset? LockedAt { get; set; }

    /// <summary>
    /// 锁屏密码的哈希（本会话一次性，解锁即清除）
    /// </summary>
    [SugarColumn(ColumnName = "lock_password_hash", Length = 256, IsNullable = true, ColumnDescription = "锁屏密码的哈希（本会话一次性，解锁即清除）")]
    public string? LockPasswordHash { get; set; }

    /// <summary>
    /// 本次锁屏连续解锁失败的次数
    /// </summary>
    [SugarColumn(ColumnName = "unlock_failed_attempts", IsNullable = false, ColumnDescription = "本次锁屏连续解锁失败的次数")]
    public int UnlockFailedAttempts { get; set; }

    /// <summary>
    /// 当前是否有效：未吊销且未过期
    /// </summary>
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && RefreshExpiresAt > now;

    /// <summary>
    /// 当前是否因为要先改密而锁定
    /// </summary>
    public bool IsPasswordChangeRequired() => string.Equals(LockReason, SessionLockReasons.PasswordChangeRequired, StringComparison.Ordinal);
}

/// <summary>
/// 会话的锁定原因
/// </summary>
/// <remarks>
/// 与前端 <c>session-lock.ts</c> 的常量一致。锁定是通用能力，锁屏只是一种；别的原因不能用锁屏密码解开
/// </remarks>
public static class SessionLockReasons
{
    /// <summary>
    /// 锁屏
    /// </summary>
    public const string ScreenLock = "ScreenLock";

    /// <summary>
    /// 强制改密
    /// </summary>
    /// <remarks>
    /// 登录时写入（系统参数 <c>identity.password</c> 打开、密码由他人设置），解开它的办法就是改密
    /// </remarks>
    public const string PasswordChangeRequired = "PasswordChangeRequired";

    /// <summary>
    /// 锁屏连错几次即吊销会话
    /// </summary>
    public const int MaxUnlockAttempts = 5;
}
