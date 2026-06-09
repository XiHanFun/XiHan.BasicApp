#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuPageQueryDto
// Guid:8dd47d74-84d5-44d5-9010-4a3e2989cd8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 菜单分页查询 DTO
/// </summary>
public sealed class MenuPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（菜单编码、名称、路由、组件、标题、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 父级菜单主键
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 权限主键
    /// </summary>
    public long? PermissionId { get; set; }

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType? MenuType { get; set; }

    /// <summary>
    /// 是否外链
    /// </summary>
    public bool? IsExternal { get; set; }

    /// <summary>
    /// 是否显示
    /// </summary>
    public bool? IsVisible { get; set; }

    /// <summary>
    /// 是否全局菜单
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
