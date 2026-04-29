#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleSelectItemDto
// Guid:6d6d6b99-9337-4d77-bcb8-0d6d970050f2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色选择项 DTO
/// </summary>
public sealed class RoleSelectItemDto : BasicAppDto
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
    /// 角色类型
    /// </summary>
    public RoleType RoleType { get; set; }

    /// <summary>
    /// 是否全局角色
    /// </summary>
    public bool IsGlobal { get; set; }
}
