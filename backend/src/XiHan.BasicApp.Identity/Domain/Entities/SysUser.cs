// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户账号实体
/// </summary>
/// <remarks>
/// 账号跟着人走、不属于某个租户，所以落平台库、不实现 <c>IMultiTenant</c>：用 <see cref="HomeTenantId"/> 记注册地（平台账号为 <c>platform</c>），登录这类还没有租户上下文的操作按它找人；同一个人进别的租户靠 <c>SysTenantMember</c>，不另建账号
/// 登录名只在注册地内唯一，由部分唯一索引 <c>ux_sys_user_home_tenant_id_user_name</c> 只约束未删除的行：不同注册地可以有同名账号，软删除后同名可以重建
/// 密码散列、失败计数、锁定与双因素拆到一对一的 <see cref="SysUserSecurity"/>：用户列表、详情与各包对用户的读取碰不到散列与密钥
/// 两种锁分开存：管理员锁定改 <see cref="Status"/>，要人工解锁；连续登录失败只写 <see cref="SysUserSecurity.LockoutEndAt"/>，到点自动解除
/// 停用、锁定与删除都同时吊销该用户的全部会话：会话门禁虽然也核对用户状态，但判定有缓存，吊销让门禁缓存立即失效并推送下线
/// <see cref="IsSystemAccount"/> 标出种子写的超级管理员与租户开通时的管理员，只由种子与开通租户管理员时标记，用户管理里的停用、锁定、删除与重置都跳过它们
/// 软删除不级联：管理员删除只删本行，本人注销连同账号安全一起删；会话、凭证以及各包按用户 ID 的引用都留着，账号不在了这些行也就用不上
/// <see cref="LastLoginAt"/> 与 <see cref="LastLoginIp"/> 在开会话时写，模仿登录不写：那不是本人的一次登录
/// 头像引用 Files 平台侧的公开文件（<see cref="AvatarFileId"/>），跟着人走、不计入租户的存储配额；换掉时经 Contracts 的 <c>IAvatarFiles</c> 回收旧文件，Identity 不直接依赖 Files
/// 索引都以注册地开头，因为对用户的读法几乎都先限定注册地：列表按创建时间、验证码登录与找回密码按手机号，也服务按注册地计数
/// </remarks>
[SugarTable(TableName = "sys_user", TableDescription = "用户账号实体")]
[ActiveUniqueIndex(nameof(HomeTenantId), nameof(UserName), Name = "ux_sys_user_home_tenant_id_user_name")]
[SugarIndex("ix_sys_user_home_tenant_id_created_at", nameof(HomeTenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[SugarIndex("ix_sys_user_home_tenant_id_phone_number", nameof(HomeTenantId), OrderByType.Asc, nameof(PhoneNumber), OrderByType.Asc)]
public sealed class SysUser : AppAuditedEntity
{
    /// <summary>
    /// 登录名
    /// </summary>
    /// <remarks>
    /// 租户内唯一，小写
    /// </remarks>
    [SugarColumn(ColumnName = "user_name", Length = 512, IsNullable = false, ColumnDescription = "登录名")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名
    /// </summary>
    [SugarColumn(ColumnName = "display_name", Length = 512, IsNullable = false, ColumnDescription = "显示名")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(ColumnName = "email", Length = 512, IsNullable = true, ColumnDescription = "邮箱")]
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    [SugarColumn(ColumnName = "phone_number", Length = 512, IsNullable = true, ColumnDescription = "手机号")]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 邮箱是否已验证
    /// </summary>
    /// <remarks>
    /// 本人收码验证或换绑后为真；管理员改了邮箱即复位
    /// </remarks>
    [SugarColumn(ColumnName = "email_verified", IsNullable = false, ColumnDescription = "邮箱是否已验证")]
    public bool EmailVerified { get; set; }

    /// <summary>
    /// 手机号是否已验证
    /// </summary>
    /// <remarks>
    /// 本人收码验证或换绑后为真；管理员改了手机号即复位
    /// </remarks>
    [SugarColumn(ColumnName = "phone_verified", IsNullable = false, ColumnDescription = "手机号是否已验证")]
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// 注册地租户 ID
    /// </summary>
    /// <remarks>
    /// 平台账号为 <c>platform</c>
    /// </remarks>
    [SugarColumn(ColumnName = "home_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "注册地租户 ID")]
    public string HomeTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// 是否系统内置账号
    /// </summary>
    /// <remarks>
    /// 初始超级管理员与租户开通时的管理员。不能停用、删除、锁定或由管理员重置密码，本人也不能改用户名或关闭账号
    /// </remarks>
    [SugarColumn(ColumnName = "is_system_account", IsNullable = false, ColumnDescription = "是否系统内置账号")]
    public bool IsSystemAccount { get; set; }

    /// <summary>
    /// 最近一次登录成功的时间
    /// </summary>
    [SugarColumn(ColumnName = "last_login_at", IsNullable = true, ColumnDescription = "最近一次登录成功的时间")]
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// 最近一次登录成功的 IP
    /// </summary>
    /// <remarks>
    /// 用户列表的一列。不遮掩（在线用户页的会话 IP 是遮掩的）
    /// </remarks>
    [SugarColumn(ColumnName = "last_login_ip", Length = 512, IsNullable = true, ColumnDescription = "最近一次登录成功的 IP")]
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    /// <remarks>
    /// 个人资料
    /// </remarks>
    [SugarColumn(ColumnName = "real_name", Length = 50, IsNullable = true, ColumnDescription = "真实姓名")]
    public string? RealName { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    [SugarColumn(ColumnName = "gender", IsNullable = false, ColumnDescription = "性别")]
    public UserGender Gender { get; set; }

    /// <summary>
    /// 生日
    /// </summary>
    [SugarColumn(ColumnName = "birthday", IsNullable = true, ColumnDescription = "生日")]
    public DateTimeOffset? Birthday { get; set; }

    /// <summary>
    /// 国家或地区
    /// </summary>
    [SugarColumn(ColumnName = "country", Length = 50, IsNullable = true, ColumnDescription = "国家或地区")]
    public string? Country { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    /// <remarks>
    /// 一列两用：管理员在用户表单里叫备注，本人在个人中心叫个人简介
    /// </remarks>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }

    /// <summary>
    /// 头像文件
    /// </summary>
    /// <remarks>
    /// Files 平台侧的公开级别文件。头像跟着用户走、不属于某个租户，不计入租户的存储配额
    /// </remarks>
    [SugarColumn(ColumnName = "avatar_file_id", IsNullable = true, ColumnDescription = "头像文件")]
    public Guid? AvatarFileId { get; set; }
}

/// <summary>
/// 性别
/// </summary>
/// <remarks>
/// 个人资料。JSON 上是字符串枚举
/// </remarks>
public enum UserGender
{
    /// <summary>
    /// 未设置
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 男
    /// </summary>
    Male = 1,

    /// <summary>
    /// 女
    /// </summary>
    Female = 2,
}

/// <summary>
/// 用户状态
/// </summary>
/// <remarks>
/// JSON 上是字符串枚举
/// </remarks>
public enum UserStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Active = 0,

    /// <summary>
    /// 已禁用，不能登录
    /// </summary>
    Disabled = 1,

    /// <summary>
    /// 已锁定（安全策略触发），待解锁
    /// </summary>
    Locked = 2,
}
