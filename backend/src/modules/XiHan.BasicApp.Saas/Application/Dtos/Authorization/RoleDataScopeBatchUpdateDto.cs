// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色数据范围批量变更中的单条授予项
/// </summary>
public sealed class RoleDataScopeBatchGrantItemDto
{
    /// <summary>
    /// 部门主键
    /// </summary>
    public long DepartmentId { get; set; }

    /// <summary>
    /// 是否包含下级部门
    /// </summary>
    public bool IncludeChildren { get; set; }
}

/// <summary>
/// 角色数据范围批量变更 DTO（一次性提交本次授权改动）
/// </summary>
public sealed class RoleDataScopeBatchUpdateDto
{
    /// <summary>
    /// 角色主键
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 待授予的部门（含是否包含下级）；已授予的部门再次下发即改其含下级
    /// </summary>
    public List<RoleDataScopeBatchGrantItemDto> Grants { get; set; } = [];

    /// <summary>
    /// 待撤销的角色数据范围记录主键集合
    /// </summary>
    public List<long> RevokeRoleDataScopeIds { get; set; } = [];
}
