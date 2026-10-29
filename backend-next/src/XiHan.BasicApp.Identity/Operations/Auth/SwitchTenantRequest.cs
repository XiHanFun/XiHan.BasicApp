// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Auth;

/// <summary>
/// <c>auth.switch-tenant</c> 的请求。
/// </summary>
/// <remarks>
/// 目标租户；为空即回到平台（只有平台账号可以）。
/// </remarks>
/// <param name="TenantId">目标租户 ID；不填或填 <c>platform</c> 即平台。</param>
public sealed record SwitchTenantRequest([RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string? TenantId = null);
