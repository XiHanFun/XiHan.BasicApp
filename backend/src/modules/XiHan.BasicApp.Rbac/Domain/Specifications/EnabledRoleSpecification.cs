using System.Linq.Expressions;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Specifications;

namespace XiHan.BasicApp.Rbac.Domain.Specifications;

/// <summary>
/// 启用角色规约
/// </summary>
public sealed class EnabledRoleSpecification : Specification<SysRole>
{
    /// <summary>
    /// 启用角色规约表达式
    /// </summary>
    /// <returns></returns>
    public override Expression<Func<SysRole, bool>> ToExpression()
    {
        return role => role.Status == YesOrNo.Yes;
    }
}
