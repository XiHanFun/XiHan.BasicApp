// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Saas.Domain.Specifications;

/// <summary>
/// 可进入的租户规约：正常、已完成配置、未删除、未过期
/// </summary>
/// <remarks>登录落点、切换租户、控制中心的可切换列表同一口径。</remarks>
public sealed class AvailableTenantSpecification(DateTimeOffset now) : Specification<SysTenant>
{
    /// <summary>
    /// 转换为表达式
    /// </summary>
    /// <returns>查询表达式</returns>
    public override Expression<Func<SysTenant, bool>> ToExpression()
    {
        return tenant => !tenant.IsDeleted
                         && tenant.TenantStatus == TenantStatus.Normal
                         && tenant.ConfigStatus == TenantConfigStatus.Configured
                         && (!tenant.ExpirationTime.HasValue || tenant.ExpirationTime.Value > now);
    }
}
