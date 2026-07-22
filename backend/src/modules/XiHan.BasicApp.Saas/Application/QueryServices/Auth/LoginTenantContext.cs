// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 登录租户上下文
/// </summary>
/// <param name="TenantId">租户标识</param>
/// <param name="TenantName">租户名称</param>
public sealed record LoginTenantContext(long TenantId, string TenantName);
