// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// 角色授予、拒绝与撤销权限的请求。
/// </summary>
/// <remarks>
/// <c>roles.grant / roles.deny / roles.revoke</c> 共用：角色与一批权限码。标识由路由值 <c>{id}</c> 绑定。
/// 授予与拒绝可带有效期与原因（撤销忽略它们）；授予不带有效期也不带原因即普通授予。
/// </remarks>
/// <param name="Id">角色 ID。</param>
/// <param name="Permissions">权限码，1 到 200 个。</param>
/// <param name="EffectiveAt">生效时间；为空即立即生效。</param>
/// <param name="ExpiresAt">到期时间（不含）；为空即长期有效。</param>
/// <param name="Reason">原因。</param>
public sealed record RolePermissionsRequest(
    Guid Id,
    [Required, MinLength(1), MaxLength(200)] IReadOnlyList<string> Permissions,
    DateTimeOffset? EffectiveAt = null,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(512)] string? Reason = null);
