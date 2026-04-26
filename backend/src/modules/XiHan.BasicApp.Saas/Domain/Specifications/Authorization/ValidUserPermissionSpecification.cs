#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ValidUserPermissionSpecification
// Guid:a87bf3f0-5532-47ec-b990-894f72e5b7472
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
