// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 有效角色权限规约
/// </summary>
public sealed class ValidRolePermissionSpecification(DateTimeOffset now) : Specification<SysRolePermission>
{
    /// <inheritdoc />
    public override Expression<Func<SysRolePermission, bool>> ToExpression()
    {
        return rolePermission => rolePermission.Status == ValidityStatus.Valid
                                 && (!rolePermission.EffectiveTime.HasValue || rolePermission.EffectiveTime.Value <= now)
                                 && (!rolePermission.ExpirationTime.HasValue || rolePermission.ExpirationTime.Value > now);
    }
}
