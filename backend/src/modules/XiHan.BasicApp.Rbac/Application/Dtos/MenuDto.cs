#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuDto
// Guid:95de1acc-af1d-4d76-b47c-2ea233daafc8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:43:06
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 菜单 DTO
/// </summary>
public class MenuDto : BasicAppDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public long? ResourceId { get; set; }

    /// <summary>
    /// 父级ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 菜单名称
    /// </summary>
    public string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 菜单编码
    /// </summary>
    public string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 菜单类型
    /// </summary>
    public MenuType MenuType { get; set; } = MenuType.Directory;

    /// <summary>
    /// 路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 组件
    /// </summary>
    public string? Component { get; set; }

    /// <summary>
    /// 图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }
}
