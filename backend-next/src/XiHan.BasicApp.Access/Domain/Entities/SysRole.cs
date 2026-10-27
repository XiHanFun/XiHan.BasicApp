// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色（平台库实体，不实现 <c>IMultiTenant</c>）：<see cref="OwnerTenantId"/> 为 <c>platform</c> 的是平台角色，否则是该租户的角色。
/// 授予写在框架的 <c>PermissionGrantRecord</c>，主体是 <c>role:&lt;角色 ID&gt;</c>（跨租户不撞），租户列按所属侧；角色码在同一侧唯一且创建后不可改。
/// </summary>
public sealed class SysRole : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 角色码：小写字母开头，只含小写字母、数字、<c>-</c>。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 显示名。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 内建（种子写入）：不能删除。
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public RoleStatus Status { get; set; } = RoleStatus.Active;
}
