// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户访问领域服务
/// </summary>
public sealed class TenantAccessDomainService : ITenantAccessDomainService
{
    /// <inheritdoc />
    public bool CanAccess(TenantMemberSnapshot member, DateTimeOffset now)
    {
        return member.InviteStatus == TenantMemberInviteStatus.Accepted
               && member.Status == ValidityStatus.Valid
               && member.Period.IsActive(now);
    }

    /// <inheritdoc />
    public bool IsPlatformAdmin(TenantMemberSnapshot member, DateTimeOffset now)
    {
        return CanAccess(member, now) && member.MemberType == TenantMemberType.PlatformAdmin;
    }
}
