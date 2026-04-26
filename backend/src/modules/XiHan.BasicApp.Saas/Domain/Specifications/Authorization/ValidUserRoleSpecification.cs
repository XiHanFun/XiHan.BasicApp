#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ValidUserRoleSpecification
// Guid:3975a1bf-d30b-42d7-9a0e-f30955b97b86
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
