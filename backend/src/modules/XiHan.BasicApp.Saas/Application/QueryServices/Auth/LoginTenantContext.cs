#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginTenantContext
// Guid:6b0b4501-bc1f-49f7-a6bf-d0dc53f4fd04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 登录租户上下文
/// </summary>
/// <param name="TenantId">租户标识</param>
/// <param name="TenantName">租户名称</param>
public sealed record LoginTenantContext(long TenantId, string TenantName);
