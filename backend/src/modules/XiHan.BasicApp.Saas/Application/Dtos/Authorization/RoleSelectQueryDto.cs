#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleSelectQueryDto
// Guid:3de95513-5557-4da5-b096-3aa026bda084
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色选择器查询 DTO
/// </summary>
public sealed class RoleSelectQueryDto
{
    /// <summary>
    /// 关键字（角色编码、名称、描述）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType? RoleType { get; set; }

    /// <summary>
    /// 是否只返回全局角色
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 返回数量上限
    /// </summary>
    public int Limit { get; set; } = 100;
}
