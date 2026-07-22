// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 有效用户角色规约
/// </summary>
public sealed class ValidUserRoleSpecification(DateTimeOffset now) : Specification<SysUserRole>
{
    /// <inheritdoc />
    public override Expression<Func<SysUserRole, bool>> ToExpression()
    {
        return userRole => userRole.Status == ValidityStatus.Valid
                           && (!userRole.EffectiveTime.HasValue || userRole.EffectiveTime.Value <= now)
                           && (!userRole.ExpirationTime.HasValue || userRole.ExpirationTime.Value > now);
    }
}
