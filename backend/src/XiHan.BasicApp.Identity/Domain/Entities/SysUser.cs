// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户账号实体
/// </summary>
/// <remarks>
/// 平台库，表 <c>sys_user</c>
/// 账号域实体不实现 <c>IMultiTenant</c>，用 <see cref="HomeTenantId"/>（注册地租户，平台账号为 <c>platform</c>）列标识归属，登录等无租户阶段的操作按它查
/// 唯一索引 <c>(home_tenant_id, user_name, is_deleted)</c>，删除后同名可重建。密码与锁定在 <see cref="SysUserSecurity"/>
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
