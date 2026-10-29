// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.update</c> 的请求：改名称、说明、到期与排序（租户 ID 不可改，状态走 suspend / resume）。标识由路由值 <c>{id}</c> 绑定。
/// </summary>
/// <param name="Id">租户主键。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明，可空。</param>
/// <param name="ExpiresAt">到期时间（UTC），可空。</param>
/// <param name="Order">排序号。</param>
/// <param name="UserLimit">租户自己的席位上限，覆盖套餐的；为空即沿用套餐。</param>
/// <param name="StorageLimitMb">租户自己的存储空间上限（MB），覆盖套餐的；为空即沿用套餐。</param>
public sealed record UpdateTenantRequest(
    Guid Id,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    [StringLength(512)] string? Description = null,
    DateTimeOffset? ExpiresAt = null,
    [Range(0, 100_000)] int Order = 0,
    [Range(0, 10_000_000)] int? UserLimit = null,
    [Range(0, 1_000_000_000)] long? StorageLimitMb = null);
