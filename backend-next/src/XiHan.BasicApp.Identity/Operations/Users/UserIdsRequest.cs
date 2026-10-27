// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 批量操作（<c>users.delete / lock / unlock</c>）的请求：集合入参，单条是集合的特例（批量优先）。
/// </summary>
/// <param name="Ids">目标用户 ID，1 到 100 个。</param>
public sealed record UserIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
