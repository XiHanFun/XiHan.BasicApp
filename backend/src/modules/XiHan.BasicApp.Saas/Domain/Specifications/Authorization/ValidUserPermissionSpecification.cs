// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 有效用户权限规约
/// </summary>
public sealed class ValidUserPermissionSpecification(DateTimeOffset now) : Specification<SysUserPermission>
{
    /// <inheritdoc />
    public override Expression<Func<SysUserPermission, bool>> ToExpression()
    {
        return userPermission => userPermission.Status == ValidityStatus.Valid
                                 && (!userPermission.EffectiveTime.HasValue || userPermission.EffectiveTime.Value <= now)
                                 && (!userPermission.ExpirationTime.HasValue || userPermission.ExpirationTime.Value > now);
    }
}
