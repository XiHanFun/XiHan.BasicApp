#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleByCodeQuery
// Guid:b2221922-866d-4961-b008-655888215c51
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:45:32
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Queries;

/// <summary>
/// 根据编码查询角色
/// </summary>
public class RoleByCodeQuery
{
    /// <summary>
    /// 角色编码
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}
