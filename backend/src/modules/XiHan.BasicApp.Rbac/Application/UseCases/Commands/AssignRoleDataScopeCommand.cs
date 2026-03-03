#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AssignRoleDataScopeCommand
// Guid:f5d22d8e-89cc-45f9-a2f7-f72d8b35a8e2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 16:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.UseCases.Commands;

/// <summary>
/// 分配角色自定义数据范围命令
/// </summary>
public class AssignRoleDataScopeCommand
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 部门ID集合
    /// </summary>
    public IReadOnlyCollection<long> DepartmentIds { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}
