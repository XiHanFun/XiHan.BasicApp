// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.change-edition</c> 的请求。
/// </summary>
/// <remarks>
/// 标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">租户主键。</param>
/// <param name="EditionId">新套餐；为空即不受套餐限制（显式选择，不是缺省回退）。</param>
public sealed record ChangeTenantEditionRequest(Guid Id, Guid? EditionId = null);
