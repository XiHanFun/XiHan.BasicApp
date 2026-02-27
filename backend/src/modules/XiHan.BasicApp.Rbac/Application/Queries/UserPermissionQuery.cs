#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionQuery
// Guid:07ffd150-7d9f-4004-ab22-00c1474a3e5a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:45:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Queries;

/// <summary>
/// 用户权限查询
/// </summary>
public class UserPermissionQuery
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}
