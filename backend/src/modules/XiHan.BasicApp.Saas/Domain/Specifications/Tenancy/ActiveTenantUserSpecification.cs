// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 有效租户成员规约
/// </summary>
public sealed class ActiveTenantUserSpecification(DateTimeOffset now) : Specification<SysTenantUser>
{
    /// <inheritdoc />
    public override Expression<Func<SysTenantUser, bool>> ToExpression()
    {
        return member => !member.IsDeleted
                         && member.InviteStatus == TenantMemberInviteStatus.Accepted
                         && member.Status == ValidityStatus.Valid
                         && (!member.EffectiveTime.HasValue || member.EffectiveTime.Value <= now)
                         && (!member.ExpirationTime.HasValue || member.ExpirationTime.Value > now);
    }
}
