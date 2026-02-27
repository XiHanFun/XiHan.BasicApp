#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantByCodeQuery
// Guid:1fcba267-3d52-4ed9-b95d-176e0d82c023
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:45:26
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Queries;

/// <summary>
/// 根据编码查询租户
/// </summary>
public class TenantByCodeQuery
{
    /// <summary>
    /// 租户编码
    /// </summary>
    public string TenantCode { get; set; } = string.Empty;
}
