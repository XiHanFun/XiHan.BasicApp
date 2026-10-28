// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Identity.Domain.Entities;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.members</c> 的响应。
/// </summary>
/// <param name="Items">成员（最多前 500 个，按用户名排序）。</param>
/// <param name="Total">成员总数。</param>
public sealed record RoleMembersResponse(IReadOnlyList<RoleMemberResponse> Items, long Total);

/// <summary>
/// 一个成员。
/// </summary>
/// <param name="UserId">用户 ID。</param>
/// <param name="UserName">登录名。</param>
/// <param name="DisplayName">显示名。</param>
/// <param name="Status">用户状态。</param>
/// <param name="AssignedAt">指派时间（UTC）。</param>
/// <param name="EffectiveAt">生效时间；为空即立即生效。</param>
/// <param name="ExpiresAt">到期时间（不含）；为空即长期有效。</param>
public sealed record RoleMemberResponse(Guid UserId, string UserName, string DisplayName, UserStatus Status, DateTimeOffset AssignedAt, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt);
