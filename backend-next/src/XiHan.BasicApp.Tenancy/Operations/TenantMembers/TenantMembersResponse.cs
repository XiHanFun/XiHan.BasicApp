// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// <c>tenant-members.list</c> 的响应：某租户的全部成员。
/// </summary>
/// <param name="Items">成员。</param>
public sealed record TenantMembersResponse(IReadOnlyList<TenantMemberResponse> Items);
