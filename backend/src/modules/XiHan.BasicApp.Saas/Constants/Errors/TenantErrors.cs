#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantErrors
// Guid:de7cb9cf-c6eb-4cfa-db0c-b78c9d0e1f2a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/06 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Constants.Errors;

/// <summary>
/// 租户模块错误消息
/// </summary>
public static class TenantErrors
{
    /// <summary>
    /// 租户不存在
    /// </summary>
    public const string NotFound = "租户不存在";

    /// <summary>
    /// 租户编码已存在
    /// </summary>
    public const string CodeExists = "租户编码已存在";

    /// <summary>
    /// 域名已存在
    /// </summary>
    public const string DomainExists = "域名已存在";

    /// <summary>
    /// 租户已过期
    /// </summary>
    public const string Expired = "租户已过期";

    /// <summary>
    /// 租户已被禁用
    /// </summary>
    public const string Disabled = "租户已被禁用";
}
