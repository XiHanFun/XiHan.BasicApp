#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MenuTreeQueryDto
// Guid:e48c8294-b998-46ef-b86a-e439c74cd219
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 菜单树查询 DTO
/// </summary>
public sealed class MenuTreeQueryDto
{
    /// <summary>
    /// 关键字（菜单编码、名称、路由、组件、标题、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 是否仅返回启用菜单
    /// </summary>
    public bool OnlyEnabled { get; set; } = true;

    /// <summary>
    /// 是否包含按钮节点
    /// </summary>
    public bool IncludeButtons { get; set; } = true;

    /// <summary>
    /// 是否仅返回可见菜单
    /// </summary>
    public bool OnlyVisible { get; set; }

    /// <summary>
    /// 返回数量上限
    /// </summary>
    public int Limit { get; set; } = 2000;
}
