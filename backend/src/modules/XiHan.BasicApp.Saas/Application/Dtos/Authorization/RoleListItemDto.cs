// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色列表项 DTO
/// </summary>
public sealed class RoleListItemDto : BasicAppDto
{
    /// <summary>
    /// 角色编码
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

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
    public RoleType RoleType { get; set; }

    /// <summary>
    /// 是否全局角色
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope DataScope { get; set; }

    /// <summary>
    /// 最大成员数
    /// </summary>
    public int MaxMembers { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}
