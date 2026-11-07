// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
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
[ActiveUniqueIndex(nameof(HomeTenantId), nameof(UserName))]
[SugarIndex("ix_{table}_home_tenant_id_created_at", nameof(HomeTenantId), OrderByType.Asc, nameof(CreatedAt), OrderByType.Asc)]
[SugarIndex("ix_{table}_home_tenant_id_phone_number", nameof(HomeTenantId), OrderByType.Asc, nameof(PhoneNumber), OrderByType.Asc)]
public sealed class SysUser : AppAuditedEntity
{
    /// <summary>
    /// 登录名
    /// </summary>
    /// <remarks>
    /// 租户内唯一，小写
    /// </remarks>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 邮箱是否已验证
    /// </summary>
    /// <remarks>
    /// 本人收码验证或换绑后为真；管理员改了邮箱即复位
    /// </remarks>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// 手机号是否已验证
    /// </summary>
    /// <remarks>
    /// 本人收码验证或换绑后为真；管理员改了手机号即复位
    /// </remarks>
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// 注册地租户 ID
    /// </summary>
    /// <remarks>
    /// 平台账号为 <c>platform</c>
    /// </remarks>
    public string HomeTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 状态
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// 是否系统内置账号
    /// </summary>
    /// <remarks>
    /// 初始超级管理员与租户开通时的管理员。不能停用、删除、锁定或由管理员重置密码，本人也不能改用户名或关闭账号
    /// </remarks>
    public bool IsSystemAccount { get; set; }

    /// <summary>
    /// 最近一次登录成功的时间
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// 最近一次登录成功的 IP
    /// </summary>
    /// <remarks>
    /// 用户列表的一列。不遮掩（在线用户页的会话 IP 是遮掩的）
    /// </remarks>
    public string? LastLoginIp { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    /// <remarks>
    /// 个人资料
    /// </remarks>
    [StringLength(50)]
    public string? RealName { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender Gender { get; set; }

    /// <summary>
    /// 生日
    /// </summary>
    public DateTimeOffset? Birthday { get; set; }

    /// <summary>
    /// 国家或地区
    /// </summary>
    [StringLength(50)]
    public string? Country { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    /// <remarks>
    /// 一列两用：管理员在用户表单里叫备注，本人在个人中心叫个人简介
    /// </remarks>
    [StringLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 头像文件
    /// </summary>
    /// <remarks>
    /// Files 平台侧的公开级别文件。头像跟着用户走、不属于某个租户，不计入租户的存储配额
    /// </remarks>
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
