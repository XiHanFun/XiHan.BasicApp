// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色从上级继承来的一条权限绑定
/// </summary>
/// <remarks>
/// 只列生效的上级（经启用角色可达）此刻有效的绑定。同一权限可能来自多个上级，逐条列出；
/// 链上任一 Deny 都会让本角色拿不到该权限，即使本角色自己授予了它。
/// </remarks>
public sealed class RoleInheritedPermissionDto
{
    /// <summary>
    /// 权限主键
    /// </summary>
    public long PermissionId { get; set; }

    /// <summary>
    /// 权限编码
    /// </summary>
    public string? PermissionCode { get; set; }

    /// <summary>
    /// 权限名称
    /// </summary>
    public string? PermissionName { get; set; }

    /// <summary>
    /// 授予或拒绝
    /// </summary>
    public PermissionAction PermissionAction { get; set; }

    /// <summary>
    /// 来源上级角色主键
    /// </summary>
    public long SourceRoleId { get; set; }

    /// <summary>
    /// 来源上级角色编码
    /// </summary>
    public string? SourceRoleCode { get; set; }

    /// <summary>
    /// 来源上级角色名称
    /// </summary>
    public string? SourceRoleName { get; set; }

    /// <summary>
    /// 来源上级的继承深度：1 为直接上级
    /// </summary>
    public int Depth { get; set; }
}
