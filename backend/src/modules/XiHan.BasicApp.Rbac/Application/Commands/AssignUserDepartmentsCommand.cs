#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AssignUserDepartmentsCommand
// Guid:9487e480-b76d-4005-a26b-2cc4976403f8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:45:01
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 分配用户部门命令
/// </summary>
public class AssignUserDepartmentsCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public IReadOnlyCollection<long> DepartmentIds { get; set; } = [];

    /// <summary>
    /// 主部门ID
    /// </summary>
    public long? MainDepartmentId { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}
