using System.Linq.Expressions;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Rbac.Domain.Specifications;

/// <summary>
/// 活跃用户规约
/// </summary>
public sealed class ActiveUserSpecification : Specification<SysUser>
{
    /// <summary>
    /// 活跃用户规约表达式
    /// </summary>
    /// <returns></returns>
    public override Expression<Func<SysUser, bool>> ToExpression()
    {
        return user => user.Status == YesOrNo.Yes;
    }
}
