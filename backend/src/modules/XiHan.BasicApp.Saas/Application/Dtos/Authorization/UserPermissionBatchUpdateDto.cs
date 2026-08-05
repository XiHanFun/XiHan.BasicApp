// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 用户直授权限批量变更中的单条授予项
/// </summary>
public sealed class UserPermissionBatchGrantItemDto
{
    /// <summary>
    /// 权限主键
    /// </summary>
    public long PermissionId { get; set; }

    /// <summary>
    /// 授权动作（授予 / 拒绝）
    /// </summary>
    public PermissionAction PermissionAction { get; set; }
}

/// <summary>
/// 用户直授权限批量变更输入
/// </summary>
public sealed class UserPermissionBatchUpdateDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 待授予的权限集合（含授权动作）
    /// </summary>
    public List<UserPermissionBatchGrantItemDto> Grants { get; set; } = [];

    /// <summary>
    /// 待撤销的用户直授权限记录主键集合
    /// </summary>
    public List<long> RevokeUserPermissionIds { get; set; } = [];
}
