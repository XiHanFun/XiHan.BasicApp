// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.assign / roles.unassign</c> 的请求。
/// </summary>
/// <remarks>
/// 角色与一批用户。标识由路由值 <c>{id}</c> 绑定。
/// </remarks>
/// <param name="Id">角色 ID。</param>
/// <param name="UserIds">用户 ID，1 到 100 个；用户的注册地必须与角色同一侧。</param>
/// <param name="EffectiveAt">新指派的生效时间；为空即立即生效（已是成员的不改，改有效期走 <c>roles.update-member</c>）。</param>
/// <param name="ExpiresAt">新指派的到期时间（不含）；为空即长期有效。</param>
public sealed record RoleMembersRequest(Guid Id, [Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> UserIds, DateTimeOffset? EffectiveAt = null, DateTimeOffset? ExpiresAt = null);
