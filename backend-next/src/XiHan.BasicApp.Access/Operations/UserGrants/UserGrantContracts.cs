// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Access.Domain.Entities;

namespace XiHan.BasicApp.Access.Operations.UserGrants;

/// <summary>
/// <c>user-grants.list</c> 的请求：一个用户（查询串 <c>userId</c>）。
/// </summary>
/// <param name="UserId">用户 ID。</param>
public sealed record UserGrantsRequest([Required] Guid UserId);

/// <summary>
/// <c>user-grants.grant / deny / revoke</c> 的请求。
/// </summary>
/// <remarks>
/// 用户与一批权限码；授予与拒绝可带有效期与原因（撤销忽略它们）。
/// </remarks>
/// <param name="UserId">用户 ID；注册地须是当前这一侧。</param>
/// <param name="Permissions">权限码，1 到 200 个。</param>
/// <param name="EffectiveAt">生效时间；为空即立即生效。</param>
/// <param name="ExpiresAt">到期时间（不含）；为空即长期有效。</param>
/// <param name="Reason">原因。</param>
public sealed record UserPermissionsRequest(
    [Required] Guid UserId,
    [Required, MinLength(1), MaxLength(200)] IReadOnlyList<string> Permissions,
    DateTimeOffset? EffectiveAt = null,
    DateTimeOffset? ExpiresAt = null,
    [StringLength(512)] string? Reason = null);

/// <summary>
/// 一个用户在当前这一侧的直接授权。
/// </summary>
/// <param name="UserId">用户 ID。</param>
/// <param name="Grants">直接授予与拒绝，按权限码排序。</param>
public sealed record UserGrantsResponse(Guid UserId, IReadOnlyList<UserGrantResponse> Grants);

/// <summary>
/// 一条直接授权。
/// </summary>
/// <param name="Permission">权限码（或 <c>*</c>）。</param>
/// <param name="Action">授予还是拒绝。</param>
/// <param name="EffectiveAt">生效时间。</param>
/// <param name="ExpiresAt">到期时间（不含）。</param>
/// <param name="Reason">原因。</param>
public sealed record UserGrantResponse(string Permission, PermissionAction Action, DateTimeOffset? EffectiveAt, DateTimeOffset? ExpiresAt, string? Reason);
