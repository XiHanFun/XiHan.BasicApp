#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AssignRolesToUserDto
// Guid:a1b2c3d4-e5f6-7890-1234-567890abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Dtos.Users;

/// <summary>
/// 用户分配角色DTO
/// </summary>
public class AssignRolesToUserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 角色ID列表
    /// </summary>
    public List<long> RoleIds { get; set; } = [];
}
