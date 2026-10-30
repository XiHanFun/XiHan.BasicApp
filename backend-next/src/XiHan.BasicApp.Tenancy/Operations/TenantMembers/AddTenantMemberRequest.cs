// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Tenancy.Domain.Entities;

namespace XiHan.BasicApp.Tenancy.Operations.TenantMembers;

/// <summary>
/// <c>tenant-members.create</c> 的请求：把一个用户加为某租户的成员。
/// </summary>
/// <param name="TenantId">租户 ID（目录键）。</param>
/// <param name="UserId">用户 ID；注册地不能就是该租户（注册地用户天然可进入）。</param>
/// <param name="MemberType">成员类型；缺省普通成员。平台账号只能是运维支持，运维支持只能是平台账号。</param>
/// <param name="EffectiveAt">生效时间（UTC）；为空即加入即生效。</param>
/// <param name="ExpiresAt">到期时间（UTC）；为空即不到期，须晚于生效时间。</param>
public sealed record AddTenantMemberRequest(
    [Required, RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string TenantId,
    Guid UserId,
    TenantMemberType MemberType = TenantMemberType.Member,
    DateTimeOffset? EffectiveAt = null,
    DateTimeOffset? ExpiresAt = null);
