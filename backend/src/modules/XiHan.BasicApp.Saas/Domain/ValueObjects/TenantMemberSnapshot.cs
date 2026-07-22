// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 租户成员快照
/// </summary>
public sealed record TenantMemberSnapshot(
    long TenantId,
    long UserId,
    TenantMemberType MemberType,
    TenantMemberInviteStatus InviteStatus,
    ValidityStatus Status,
    EffectivePeriod Period);
