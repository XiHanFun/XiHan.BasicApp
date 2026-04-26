#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberSnapshot
// Guid:f279ddc6-fd9d-4642-b34a-fd469c9bc7df
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
