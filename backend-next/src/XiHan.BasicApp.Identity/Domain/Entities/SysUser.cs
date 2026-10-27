// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Identity.Domain.Entities;

/// <summary>
/// 用户（平台库，表 <c>sys_user</c>）：账号域实体不实现 <c>IMultiTenant</c>，用 <see cref="HomeTenantId"/>（注册地租户）列标识归属，登录等无租户阶段的操作按它查。
/// 唯一索引 <c>(home_tenant_id, user_name, is_deleted)</c>，删除后同名可重建。
/// </summary>
public sealed class SysUser : AppAuditedEntity
{
    /// <summary>登录名；租户内唯一，小写。</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>显示名。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>邮箱。</summary>
    public string? Email { get; set; }

    /// <summary>注册地租户 ID。</summary>
    public string HomeTenantId { get; set; } = string.Empty;

    /// <summary>状态。</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;
}
