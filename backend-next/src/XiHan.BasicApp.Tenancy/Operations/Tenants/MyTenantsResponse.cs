// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.mine</c> 的响应：能否进入平台、当前是否在平台，以及可进入的租户（控制中心按它画卡片）。
/// </summary>
/// <param name="CanAccessPlatform">是否平台账号（注册地是平台）。</param>
/// <param name="PlatformIsCurrent">当前是否在平台侧（令牌不带租户）。</param>
/// <param name="Tenants">可进入的租户（只含正常状态的）。</param>
public sealed record MyTenantsResponse(bool CanAccessPlatform, bool PlatformIsCurrent, IReadOnlyList<AvailableTenantResponse> Tenants);
