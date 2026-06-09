#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ActiveTenantUserSpecification
// Guid:75e35009-b56b-45de-bce4-b022c38d3f40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
