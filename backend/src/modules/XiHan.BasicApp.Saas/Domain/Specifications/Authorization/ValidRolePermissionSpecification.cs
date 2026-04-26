#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ValidRolePermissionSpecification
// Guid:b9f5c5c8-9f73-4707-83e2-0e1810f758a1
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
