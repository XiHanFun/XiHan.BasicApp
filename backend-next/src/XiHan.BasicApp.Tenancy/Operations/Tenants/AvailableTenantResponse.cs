// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 当前用户可进入的一个租户（<c>tenants.mine</c> 的列表项）。
/// </summary>
/// <remarks>
/// 注册地租户或成员租户。
/// </remarks>
/// <param name="TenantId">租户 ID（目录键，切换时传它）。</param>
/// <param name="Name">名称。</param>
/// <param name="IsHome">是否注册地租户。</param>
/// <param name="MemberType">成员类型；注册地租户为空。</param>
/// <param name="IsOwner">是否该租户的所有者（<c>SysTenant.AdminUserId</c>）。</param>
/// <param name="IsCurrent">是否当前所在租户。</param>
/// <param name="JoinedAt">加入时间：成员是成为成员的时间，注册地租户是账号建立的时间。</param>
public sealed record AvailableTenantResponse(string TenantId, string Name, bool IsHome, TenantMemberType? MemberType, bool IsOwner, bool IsCurrent, DateTimeOffset JoinedAt);
