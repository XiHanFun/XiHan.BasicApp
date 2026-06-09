#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionUpdateDto
// Guid:f14ec243-ef12-4d3c-8c43-6b8484eb3af0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 权限定义更新 DTO
/// </summary>
public sealed class PermissionUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 权限名称
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    public string? PermissionDescription { get; set; }

    /// <summary>
    /// 权限标签
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// 是否需要审计
    /// </summary>
    public bool IsRequireAudit { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
