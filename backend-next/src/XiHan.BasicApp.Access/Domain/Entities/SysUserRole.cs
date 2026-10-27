// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 用户与角色的指派（平台库实体）：<see cref="OwnerTenantId"/> 与角色相同，用户的注册地必须是同一侧。
/// </summary>
public sealed class SysUserRole : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户 ID。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 角色 ID。
    /// </summary>
    public Guid RoleId { get; set; }
}
