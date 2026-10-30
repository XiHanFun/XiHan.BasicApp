// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Tenancy.Domain.Entities;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.configure-database</c> 的请求。
/// </summary>
/// <remarks>
/// 设隔离方式与独立库的连接；标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">租户主键。</param>
/// <param name="IsolationMode">隔离方式；改回共库即清掉连接。</param>
/// <param name="ConnectionString">独立库的连接串；独立库时必填，已配过时留空即沿用原来的。</param>
/// <param name="DatabaseProvider">独立库的库类型：<c>postgresql</c>、<c>sqlite</c>、<c>mysql</c> 或 <c>sqlserver</c>；为空即与平台库相同。</param>
public sealed record ConfigureTenantDatabaseRequest(
    Guid Id,
    TenantIsolationMode IsolationMode,
    [StringLength(1024)] string? ConnectionString = null,
    [RegularExpression("^(postgresql|sqlite|mysql|sqlserver)$")] string? DatabaseProvider = null);

/// <summary>
/// <c>tenants.initialize-database</c> 的请求。
/// </summary>
/// <remarks>
/// 标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">租户主键。</param>
public sealed record InitializeTenantDatabaseRequest(Guid Id);
