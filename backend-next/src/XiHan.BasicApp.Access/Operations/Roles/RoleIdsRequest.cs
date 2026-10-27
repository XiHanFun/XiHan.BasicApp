// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Access.Operations.Roles;

/// <summary>
/// <c>roles.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">角色 ID，1 到 100 个。</param>
public sealed record RoleIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
