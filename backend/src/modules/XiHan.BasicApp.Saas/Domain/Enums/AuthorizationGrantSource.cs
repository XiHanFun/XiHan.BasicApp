#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthorizationGrantSource
// Guid:5878fb7e-7ecf-4d92-9dd3-e3d27b7a6fd6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 授权来源
/// </summary>
public enum AuthorizationGrantSource
{
    /// <summary>
    /// 用户直授
    /// </summary>
    [Description("用户直授")]
    User = 0,

    /// <summary>
    /// 角色授权
    /// </summary>
    [Description("角色授权")]
    Role = 1,

    /// <summary>
    /// 租户版本
    /// </summary>
    [Description("租户版本")]
    TenantEdition = 2
}
