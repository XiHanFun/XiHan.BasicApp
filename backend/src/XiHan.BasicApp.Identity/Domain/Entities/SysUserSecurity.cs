// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 账号安全信息：密码散列、失败计数与锁定、安全戳与双因素
/// </summary>
/// <remarks>
/// 与 <see cref="SysUser"/> 一对一拆表：散列、密钥与计数只在登录、改密与双因素这些路径上读，用户列表与详情碰不到；标 <c>[DiffLogIgnored]</c>，这些密文与计数也不进数据变更日志
/// 行按需建：取安全行时没有即建一行空的（没有密码、不能登录），只读调用里只给出不落库的缺省行（框架拒绝只读工作单元里的写入）；部分唯一索引 <c>ux_sys_user_security_user_id</c> 保证一人一行
/// 失败计数与锁定在独立作用域里提交：登录以失败结束时调用的工作单元整体回滚，不这样就永远锁不住；锁定到 <see cref="LockoutEndAt"/> 自动解除，不改 <c>SysUser.Status</c>
/// 开了双因素的账号等第二步通过才清零计数：只知道密码的人不能靠反复登录把计数清掉
/// 认证器密钥经 <c>ISecretProtector</c> 加密存，校验验证码要明文、不能存散列；<see cref="TwoFactorLastStep"/> 记最近通过的时间步，同一步的码不能再用
/// 密码由他人设置时记下 <see cref="MustChangePassword"/>，逼不逼改看登录时的系统参数 <c>identity.password</c>：标记随密码写下，策略事后打开也对已有账号生效
/// 多端登录与设备数、开放接口的签名算法与 IP 白名单放在这里而不是各会话或各凭证上：按人设一次，作用于本人全部会话与凭证
/// 安全戳在设密码、开关双因素、本人停用账号时换新，但没有代码核对它；让既有会话失效靠吊销 <see cref="SysUserSession"/>
/// 只有本人注销账号时连同账号软删，管理员删除用户不动本行
/// </remarks>
[SugarTable(TableName = "sys_user_security", TableDescription = "账号安全信息：密码散列、失败计数与锁定、安全戳与双因素")]
[ActiveUniqueIndex(nameof(UserId), Name = "ux_sys_user_security_user_id")]
[DiffLogIgnored]
public sealed class SysUserSecurity : AppAuditedEntity
{
    /// <summary>
    /// 用户 ID（唯一）
    /// </summary>
    [SugarColumn(ColumnName = "user_id", IsNullable = false, ColumnDescription = "用户 ID（唯一）")]
    public Guid UserId { get; set; }

    /// <summary>
    /// 密码的 PBKDF2 散列
    /// </summary>
    /// <remarks>
    /// 格式 <c>pbkdf2-sha256$迭代$盐$散列</c>。空串表示尚未设置密码，不能登录
    /// </remarks>
    [SugarColumn(ColumnName = "password_hash", Length = 512, IsNullable = false, ColumnDescription = "密码的 PBKDF2 散列")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 最近一次改密时间
    /// </summary>
    [SugarColumn(ColumnName = "password_changed_at", IsNullable = true, ColumnDescription = "最近一次改密时间")]
    public DateTimeOffset? PasswordChangedAt { get; set; }

    /// <summary>
    /// 最近一次本人改用户名的时间
    /// </summary>
    /// <remarks>
    /// 90 天一次
    /// </remarks>
    [SugarColumn(ColumnName = "user_name_changed_at", IsNullable = true, ColumnDescription = "最近一次本人改用户名的时间")]
    public DateTimeOffset? UserNameChangedAt { get; set; }

    /// <summary>
    /// 自上次成功登录以来的连续失败次数
    /// </summary>
    [SugarColumn(ColumnName = "failed_access_count", IsNullable = false, ColumnDescription = "自上次成功登录以来的连续失败次数")]
    public int FailedAccessCount { get; set; }

    /// <summary>
    /// 最近一次登录失败的时间
    /// </summary>
    /// <remarks>
    /// 个人中心的安全状态显示。登录成功不清掉
    /// </remarks>
    [SugarColumn(ColumnName = "last_failed_login_at", IsNullable = true, ColumnDescription = "最近一次登录失败的时间")]
    public DateTimeOffset? LastFailedLoginAt { get; set; }

    /// <summary>
    /// 锁定截止时间
    /// </summary>
    /// <remarks>
    /// 之前的登录一律拒绝
    /// </remarks>
    [SugarColumn(ColumnName = "lockout_end_at", IsNullable = true, ColumnDescription = "锁定截止时间")]
    public DateTimeOffset? LockoutEndAt { get; set; }

    /// <summary>
    /// 安全戳：改密、重置、锁定时更新，用于使既有会话失效
    /// </summary>
    [SugarColumn(ColumnName = "security_stamp", Length = 512, IsNullable = false, ColumnDescription = "安全戳：改密、重置、锁定时更新，用于使既有会话失效")]
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>
    /// 密码由他人设置、本人还没改过
    /// </summary>
    /// <remarks>
    /// 管理员创建或重置、开通租户管理员、种子写入时为真，本人改密或找回密码后清除
    /// 只在系统参数 <c>identity.password</c> 的 <c>forceChange</c> 打开时生效：用密码登录后锁定到改密为止
    /// </remarks>
    [SugarColumn(ColumnName = "must_change_password", IsNullable = false, ColumnDescription = "密码由他人设置、本人还没改过")]
    public bool MustChangePassword { get; set; }

    /// <summary>
    /// 是否开启了双因素
    /// </summary>
    /// <remarks>
    /// 开启后密码通过还要过第二步（<see cref="TwoFactorMethods"/> 里至少有一种）
    /// </remarks>
    [SugarColumn(ColumnName = "two_factor_enabled", IsNullable = false, ColumnDescription = "是否开启了双因素")]
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// 开启的双因素方式（按位组合）
    /// </summary>
    [SugarColumn(ColumnName = "two_factor_methods", IsNullable = false, ColumnDescription = "开启的双因素方式（按位组合）")]
    public TwoFactorMethods TwoFactorMethods { get; set; }

    /// <summary>
    /// 认证器密钥
    /// </summary>
    /// <remarks>
    /// Base32，经 <c>ISecretProtector</c> 加密后存。没开认证器为空
    /// </remarks>
    [SugarColumn(ColumnName = "two_factor_secret", Length = 1024, IsNullable = true, ColumnDescription = "认证器密钥")]
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// 认证器最近一次通过的时间步：同一步（及更早的）验证码不能再用，防重放
    /// </summary>
    [SugarColumn(ColumnName = "two_factor_last_step", IsNullable = true, ColumnDescription = "认证器最近一次通过的时间步：同一步（及更早的）验证码不能再用，防重放")]
    public long? TwoFactorLastStep { get; set; }

    /// <summary>
    /// 开放接口的签名算法
    /// </summary>
    /// <remarks>
    /// 个人中心「调用安全设置」，作用于本人全部凭证。为空即 HMAC-SHA256
    /// </remarks>
    [SugarColumn(ColumnName = "api_signature_algorithm", Length = 32, IsNullable = true, ColumnDescription = "开放接口的签名算法")]
    public string? ApiSignatureAlgorithm { get; set; }

    /// <summary>
    /// 开放接口的 IP 白名单
    /// </summary>
    /// <remarks>
    /// 每行一个 IP 或 CIDR，作用于本人全部凭证。为空即不限
    /// </remarks>
    [SugarColumn(ColumnName = "api_ip_allow_list", Length = 2048, IsNullable = true, ColumnDescription = "开放接口的 IP 白名单")]
    public string? ApiIpAllowList { get; set; }

    /// <summary>
    /// 是否禁止多端登录
    /// </summary>
    /// <remarks>
    /// 为真时登录只保留最新的一个会话；缺省允许
    /// </remarks>
    [SugarColumn(ColumnName = "deny_multi_login", IsNullable = false, ColumnDescription = "是否禁止多端登录")]
    public bool DenyMultiLogin { get; set; }

    /// <summary>
    /// 最大登录设备数
    /// </summary>
    /// <remarks>
    /// 允许多端登录时生效，超过时吊销最早的会话；0 即不限
    /// </remarks>
    [SugarColumn(ColumnName = "max_login_devices", IsNullable = false, ColumnDescription = "最大登录设备数")]
    public int MaxLoginDevices { get; set; }
}

/// <summary>
/// 账号开启的双因素方式
/// </summary>
/// <remarks>
/// 按位组合，存库。接口上按 <see cref="TwoFactorMethod"/> 的列表出入
/// </remarks>
[Flags]
public enum TwoFactorMethods
{
    /// <summary>
    /// 没有开启任何方式
    /// </summary>
    None = 0,

    /// <summary>
    /// 认证器（TOTP）
    /// </summary>
    Totp = 1,

    /// <summary>
    /// 邮箱验证码
    /// </summary>
    Email = 2,

    /// <summary>
    /// 手机短信验证码
    /// </summary>
    Phone = 4,
}
