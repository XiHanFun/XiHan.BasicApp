// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 部门闭包表（租户库实体）。
/// </summary>
/// <remarks>
/// 每个部门到自己（深度 0）与到每个祖先（深度 1、2、…）各一行。
/// 创建时写入、移动时整棵子树重算、删除时连同移除；由 <see cref="Services.DepartmentService"/> 维护，操作不直接写。
/// </remarks>
public sealed class SysDepartmentHierarchy : TenantScopedEntity
{
    /// <summary>
    /// 祖先部门（深度 0 时是自己）。
    /// </summary>
    public Guid AncestorId { get; set; }

    /// <summary>
    /// 子孙部门。
    /// </summary>
    public Guid DescendantId { get; set; }

    /// <summary>
    /// 层级距离：0 是自己，1 是直接上级。
    /// </summary>
    public int Depth { get; set; }
}
