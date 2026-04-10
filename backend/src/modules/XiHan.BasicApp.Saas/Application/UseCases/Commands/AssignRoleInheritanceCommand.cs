#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AssignRoleInheritanceCommand
// Guid:9f7f5aa1-479a-44c1-bd85-44595b9d0f36
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.UseCases.Commands;

/// <summary>
/// 分配角色继承关系命令
/// </summary>
public class AssignRoleInheritanceCommand
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 父角色ID集合
    /// </summary>
    public IReadOnlyCollection<long> ParentRoleIds { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}
