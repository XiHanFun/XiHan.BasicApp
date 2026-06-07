#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RolePermissionBatchUpdateDto
// Guid:6f2b8c1d-4a3e-4f8a-9c2b-7d1e5a9b3c40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 角色权限批量变更 DTO（一次性提交本次授权改动）
/// </summary>
public sealed class RolePermissionBatchUpdateDto
{
    /// <summary>
    /// 角色主键
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 待授予的权限主键集合
    /// </summary>
    public List<long> GrantPermissionIds { get; set; } = [];

    /// <summary>
    /// 待撤销的角色权限记录主键集合
    /// </summary>
    public List<long> RevokeRolePermissionIds { get; set; } = [];
}
