// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色权限批量变更 DTO（一次性提交本次授权改动）
/// </summary>
public sealed class RolePermissionBatchUpdateDto
{
    /// <summary>
    /// 角色主键
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 待授予的权限主键集合
    /// </summary>
    public List<long> GrantPermissionIds { get; set; } = [];

    /// <summary>
    /// 待撤销的角色权限记录主键集合
    /// </summary>
    public List<long> RevokeRolePermissionIds { get; set; } = [];
}
