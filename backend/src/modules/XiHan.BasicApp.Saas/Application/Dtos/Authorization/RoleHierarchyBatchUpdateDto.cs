// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色父角色批量变更 DTO（一次性提交本次继承改动）
/// </summary>
public sealed class RoleHierarchyBatchUpdateDto
{
    /// <summary>
    /// 角色主键（继承方）
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 待新增的直接父角色主键集合
    /// </summary>
    public List<long> AddParentRoleIds { get; set; } = [];

    /// <summary>
    /// 待移除的直接父角色主键集合
    /// </summary>
    public List<long> RemoveParentRoleIds { get; set; } = [];
}
