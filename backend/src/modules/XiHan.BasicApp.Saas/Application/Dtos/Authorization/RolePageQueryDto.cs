#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePageQueryDto
// Guid:ff272d27-ac04-4e9e-bc38-e4d4ce1d7ac5
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
/// 角色分页查询 DTO
/// </summary>
public sealed class RolePageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字（角色编码、名称、描述、备注）
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 角色类型
    /// </summary>
    public RoleType? RoleType { get; set; }

    /// <summary>
    /// 数据权限范围
    /// </summary>
    public DataPermissionScope? DataScope { get; set; }

    /// <summary>
    /// 是否全局角色
    /// </summary>
    public bool? IsGlobal { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public EnableStatus? Status { get; set; }
}
