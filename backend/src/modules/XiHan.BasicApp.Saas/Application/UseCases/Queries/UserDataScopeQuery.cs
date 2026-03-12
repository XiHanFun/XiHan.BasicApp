#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeQuery
// Guid:0ddf0bf8-f8cb-44b2-8bf3-a00cfa9c9a88
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 16:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.UseCases.Queries;

/// <summary>
/// 用户数据范围查询
/// </summary>
public class UserDataScopeQuery
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
