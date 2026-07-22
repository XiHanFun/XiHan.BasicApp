// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色继承创建 DTO
/// </summary>
public sealed class RoleHierarchyCreateDto
{
    /// <summary>
    /// 祖先角色主键
    /// </summary>
    public long AncestorId { get; set; }

    /// <summary>
    /// 后代角色主键
    /// </summary>
    public long DescendantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
