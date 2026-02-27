#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AssignUserPermissionsCommand
// Guid:3d303425-1d9b-4219-9d90-02767774162e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:44:38
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 分配用户直授权限命令
/// </summary>
public class AssignUserPermissionsCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public IReadOnlyCollection<long> PermissionIds { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}
