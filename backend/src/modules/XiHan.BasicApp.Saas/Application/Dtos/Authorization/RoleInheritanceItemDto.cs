// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 继承链上的一个角色：上级链里是本角色的上级，下级链里是本角色的下级
/// </summary>
public sealed class RoleInheritanceItemDto
{
    /// <summary>
    /// 角色主键
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 角色编码
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType RoleType { get; set; }

    /// <summary>
    /// 角色状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 是否平台全局角色
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 继承深度：1 为直接继承
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// 继承路径上的角色名称，按「上级 → 下级」排列，含两端；继承生效时取生效的那条最短路径
    /// </summary>
    public List<string> PathRoleNames { get; set; } = [];

    /// <summary>
    /// 继承是否生效：上级与路径上的中间角色都已启用（本角色自身的启停不计）
    /// </summary>
    public bool IsEffective { get; set; }
}
