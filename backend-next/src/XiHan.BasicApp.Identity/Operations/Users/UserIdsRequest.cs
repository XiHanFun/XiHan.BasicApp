// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Users;

/// <summary>
/// 用户批量操作的请求。
/// </summary>
/// <remarks>
/// 用于 <c>users.delete / lock / unlock</c>。集合入参，单条是集合的特例（批量优先）。
/// </remarks>
/// <param name="Ids">目标用户 ID，1 到 100 个。</param>
public sealed record UserIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
