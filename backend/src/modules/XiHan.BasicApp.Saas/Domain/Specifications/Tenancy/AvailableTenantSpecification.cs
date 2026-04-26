#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AvailableTenantSpecification
// Guid:11d0ce4a-0a53-4394-88b4-3609cc8b4dd4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
                         && (!tenant.ExpireTime.HasValue || tenant.ExpireTime.Value > now);
    }
}
