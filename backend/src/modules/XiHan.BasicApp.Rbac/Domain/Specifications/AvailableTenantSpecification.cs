using System.Linq.Expressions;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Entities;
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
