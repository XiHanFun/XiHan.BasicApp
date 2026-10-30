// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户账号实体。
/// </summary>
/// <remarks>
/// 平台库，表 <c>sys_user</c>。
/// 账号域实体不实现 <c>IMultiTenant</c>，用 <see cref="HomeTenantId"/>（注册地租户，平台账号为 <c>platform</c>）列标识归属，登录等无租户阶段的操作按它查。
/// 唯一索引 <c>(home_tenant_id, user_name, is_deleted)</c>，删除后同名可重建。密码与锁定在 <see cref="SysUserSecurity"/>。
/// </remarks>
public sealed class SysUser : AppAuditedEntity
{
    /// <summary>
    /// 登录名。
    /// </summary>
    /// <remarks>
    /// 租户内唯一，小写。
    /// </remarks>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱。
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 手机号。
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// 邮箱是否已验证。
    /// </summary>
    /// <remarks>
    /// 本人收码验证或换绑后为真；管理员改了邮箱即复位（4.x 同）。
    /// </remarks>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// 手机号是否已验证。
    /// </summary>
    /// <remarks>
    /// 本人收码验证或换绑后为真；管理员改了手机号即复位（4.x 同）。
    /// </remarks>
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// 注册地租户 ID。
    /// </summary>
    /// <remarks>
    /// 平台账号为 <c>platform</c>。
    /// </remarks>
    public string HomeTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 状态。
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>
    /// 最近一次登录成功的时间。
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// 真实姓名。
    /// </summary>
    /// <remarks>
    /// 个人资料，4.x 同。
    /// </remarks>
    [StringLength(50)]
    public string? RealName { get; set; }

    /// <summary>
    /// 性别。
    /// </summary>
    public UserGender Gender { get; set; }

    /// <summary>
    /// 生日。
    /// </summary>
    public DateTimeOffset? Birthday { get; set; }

    /// <summary>
    /// 国家或地区。
    /// </summary>
    [StringLength(50)]
    public string? Country { get; set; }

    /// <summary>
    /// 个人简介。
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 头像文件。
    /// </summary>
    /// <remarks>
    /// Files 平台侧的公开级别文件。头像跟着用户走、不属于某个租户，不计入租户的存储配额。
    /// </remarks>
    public Guid? AvatarFileId { get; set; }
}
