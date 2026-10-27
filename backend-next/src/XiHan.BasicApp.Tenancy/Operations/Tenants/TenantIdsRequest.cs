// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 批量操作（<c>tenants.suspend / resume / delete</c>）的请求：集合入参。
/// </summary>
/// <param name="Ids">租户主键，1 到 100 个。</param>
public sealed record TenantIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
