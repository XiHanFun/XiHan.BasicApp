#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AvailableTenantSpecification
// Guid:90d66d8a-15f7-4d1a-8ef6-1929604b061d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:38:15
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Linq.Expressions;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Rbac.Domain.Specifications;

/// <summary>
/// 可用租户规约
/// </summary>
public sealed class AvailableTenantSpecification : Specification<SysTenant>
{
    /// <summary>
    /// 可用租户规约
    /// </summary>
    /// <returns></returns>
    public override Expression<Func<SysTenant, bool>> ToExpression()
    {
        return tenant => tenant.Status == YesOrNo.Yes
                         && tenant.TenantStatus == TenantStatus.Normal
                         && (tenant.ExpireTime == null || tenant.ExpireTime > DateTimeOffset.UtcNow);
    }
}
