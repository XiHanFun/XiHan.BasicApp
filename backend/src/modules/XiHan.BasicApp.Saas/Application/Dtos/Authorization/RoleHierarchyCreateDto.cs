#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleHierarchyCreateDto
// Guid:54e6de43-e173-4a8b-9c6c-39f5128b64b7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色继承创建 DTO
/// </summary>
public sealed class RoleHierarchyCreateDto
{
    /// <summary>
    /// 祖先角色主键
    /// </summary>
    public long AncestorId { get; set; }

    /// <summary>
    /// 后代角色主键
    /// </summary>
    public long DescendantId { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
