// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色更新 DTO
/// </summary>
public sealed class RoleUpdateDto : BasicAppDto
{
    /// <summary>
    /// 角色名称
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// 角色描述
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType RoleType { get; set; } = RoleType.Custom;

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope DataScope { get; set; } = DataPermissionScope.SelfOnly;

    /// <summary>
    /// 最大成员数
    /// </summary>
    public int MaxMembers { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
