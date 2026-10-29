// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 角色继承的一条直接边（平台库实体，随所属侧）。
/// </summary>
/// <remarks>
/// <see cref="RoleId"/> 继承 <see cref="ParentId"/> 的授予（方案 5.9）。
/// 只存直接上级，祖先按边广度优先展开；停用的上级截断经过它的继承。只在同一侧的角色之间，内置角色不参与；解除即删除。
/// </remarks>
public sealed class SysRoleInheritance : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 下级角色（继承者）。
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// 直接上级角色。
    /// </summary>
    public Guid ParentId { get; set; }
}
