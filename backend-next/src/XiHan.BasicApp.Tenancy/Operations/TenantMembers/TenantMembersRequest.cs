// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// <c>tenant-members.list</c> 的请求：按租户列成员。
/// </summary>
/// <param name="TenantId">租户 ID（目录键）。</param>
public sealed record TenantMembersRequest([Required, RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string TenantId);
