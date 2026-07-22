// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 可用租户规约
/// </summary>
public sealed class AvailableTenantSpecification(DateTimeOffset now) : Specification<SysTenant>
{
    /// <inheritdoc />
    public override Expression<Func<SysTenant, bool>> ToExpression()
    {
        return tenant => !tenant.IsDeleted
                         && tenant.TenantStatus == TenantStatus.Normal
                         && (!tenant.ExpirationTime.HasValue || tenant.ExpirationTime.Value > now);
    }
}
