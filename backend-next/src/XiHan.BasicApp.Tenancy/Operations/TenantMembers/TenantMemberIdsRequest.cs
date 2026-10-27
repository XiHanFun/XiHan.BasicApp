// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// <c>tenant-members.delete</c> 的请求：集合入参。
/// </summary>
/// <param name="Ids">成员行主键，1 到 100 个。</param>
public sealed record TenantMemberIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
